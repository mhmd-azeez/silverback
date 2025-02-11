﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="IConsumer" />
    public abstract class Consumer : IConsumer, IDisposable
    {
        private readonly IReadOnlyList<IConsumerBehavior> _behaviors;

        private readonly ISilverbackLogger<Consumer> _logger;

        private readonly ConsumerStatusInfo _statusInfo = new();

        private readonly ConcurrentDictionary<IBrokerMessageIdentifier, int>
            _failedAttemptsDictionary = new();

        private Task? _connectTask;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Consumer" /> class.
        /// </summary>
        /// <param name="broker">
        ///     The <see cref="IBroker" /> that is instantiating the consumer.
        /// </param>
        /// <param name="endpoint">
        ///     The endpoint to be consumed.
        /// </param>
        /// <param name="behaviorsProvider">
        ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackLogger" />.
        /// </param>
        protected Consumer(
            IBroker broker,
            IConsumerEndpoint endpoint,
            IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider,
            ISilverbackLogger<Consumer> logger)
        {
            Broker = Check.NotNull(broker, nameof(broker));
            Endpoint = Check.NotNull(endpoint, nameof(endpoint));

            _behaviors = Check.NotNull(behaviorsProvider, nameof(behaviorsProvider)).GetBehaviorsList();
            ServiceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
            _logger = Check.NotNull(logger, nameof(logger));

            Endpoint.Validate();
        }

        /// <inheritdoc cref="IConsumer.Id" />
        public InstanceIdentifier Id { get; } = new();

        /// <inheritdoc cref="IConsumer.Broker" />
        public IBroker Broker { get; }

        /// <inheritdoc cref="IConsumer.Endpoint" />
        public IConsumerEndpoint Endpoint { get; }

        /// <inheritdoc cref="IConsumer.StatusInfo" />
        public IConsumerStatusInfo StatusInfo => _statusInfo;

        /// <inheritdoc cref="IConsumer.IsConnecting" />
        public bool IsConnecting => _connectTask != null;

        /// <inheritdoc cref="IConsumer.IsConnected" />
        public bool IsConnected { get; private set; }

        /// <inheritdoc cref="IConsumer.IsConsuming" />
        public bool IsConsuming { get; protected set; }

        /// <summary>
        ///     Gets the <see cref="IServiceProvider" /> to be used to resolve the required services.
        /// </summary>
        protected IServiceProvider ServiceProvider { get; }

        /// <summary>
        ///     Gets the <see cref="ISequenceStore" /> instances used by this consumer. Some brokers will require
        ///     multiple stores (e.g. the <c>KafkaConsumer</c> will create a store per each assigned partition).
        /// </summary>
        protected IList<ISequenceStore> SequenceStores { get; } = new List<ISequenceStore>();

        /// <summary>
        ///     Gets a value indicating whether the consumer is being disconnected.
        /// </summary>
        protected bool IsDisconnecting { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether the consumer is being stopped.
        /// </summary>
        protected bool IsStopping { get; private set; }

        /// <inheritdoc cref="IConsumer.ConnectAsync" />
        public async Task ConnectAsync()
        {
            try
            {
                if (IsConnected)
                    return;

                if (_connectTask != null)
                {
                    await _connectTask.ConfigureAwait(false);
                    return;
                }

                _connectTask = ConnectCoreAsync();

                try
                {
                    await _connectTask.ConfigureAwait(false);

                    IsConnected = true;
                }
                finally
                {
                    _connectTask = null;
                }

                _statusInfo.SetConnected();
                _logger.LogConsumerConnected(this);

                await StartAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogConsumerConnectError(this, ex);
                throw;
            }
        }

        /// <inheritdoc cref="IConsumer.DisconnectAsync" />
        public async Task DisconnectAsync()
        {
            try
            {
                if (!IsConnected || IsDisconnecting)
                    return;

                _logger.LogConsumerLowLevelTrace(
                    this,
                    "Disconnecting consumer...");

                IsDisconnecting = true;

                // Ensure that StopCore is called in any case to avoid deadlocks (when the consumer loop is initialized
                // but not started)
                if (IsConsuming && !IsStopping)
                    await StopAsync().ConfigureAwait(false);
                else
                    await StopCoreAsync().ConfigureAwait(false);

                if (SequenceStores.Count > 0)
                {
                    await SequenceStores
                        .DisposeAllAsync(SequenceAbortReason.ConsumerAborted)
                        .ConfigureAwait(false);
                }

                await WaitUntilConsumingStoppedAsync().ConfigureAwait(false);
                await DisconnectCoreAsync().ConfigureAwait(false);

                SequenceStores.ForEach(store => store.Dispose());
                SequenceStores.Clear();

                IsConnected = false;
                _statusInfo.SetDisconnected();
                _logger.LogConsumerDisconnected(this);
            }
            catch (Exception ex)
            {
                _logger.LogConsumerDisconnectError(this, ex);
                throw;
            }
            finally
            {
                IsDisconnecting = false;
            }
        }

        /// <inheritdoc cref="IConsumer.DisconnectAsync" />
        public async Task TriggerReconnectAsync()
        {
            IsDisconnecting = true;

            // Ensure that StopCore is called in any case to avoid deadlocks (when the consumer loop is initialized
            // but not started)
            if (IsConsuming && !IsStopping)
                await StopAsync().ConfigureAwait(false);
            else
                await StopCoreAsync().ConfigureAwait(false);

            Task.Run(
                    async () =>
                    {
                        await WaitUntilConsumingStoppedAsync().ConfigureAwait(false);
                        await DisconnectAsync().ConfigureAwait(false);
                        await ConnectAsync().ConfigureAwait(false);
                    })
                .FireAndForget();
        }

        /// <inheritdoc cref="IConsumer.StartAsync" />
        public async Task StartAsync()
        {
            try
            {
                if (!IsConnected)
                    throw new InvalidOperationException("The consumer is not connected.");

                if (IsConsuming)
                    return;

                await _logger.ExecuteAndTraceConsumerActionAsync(
                    this,
                    StartCoreAsync,
                    "Starting consumer...",
                    "Consumer started.",
                    "Failed to start consumer.")
                    .ConfigureAwait(false);

                IsConsuming = true;
            }
            catch (Exception ex)
            {
                _logger.LogConsumerStartError(this, ex);
                throw;
            }
        }

        /// <inheritdoc cref="IConsumer.StopAsync" />
        public async Task StopAsync()
        {
            try
            {
                if (!IsConsuming || IsStopping)
                    return;

                IsStopping = true;

                await _logger.ExecuteAndTraceConsumerActionAsync(
                        this,
                        StopCoreAsync,
                        "Stopping consumer...",
                        "Consumer stopped.",
                        "Failed to stop consumer.")
                    .ConfigureAwait(false);

                IsConsuming = false;
            }
            catch (Exception ex)
            {
                _logger.LogConsumerStopError(this, ex);
                throw;
            }
            finally
            {
                IsStopping = false;
            }
        }

        /// <inheritdoc cref="IConsumer.CommitAsync(IBrokerMessageIdentifier)" />
        public Task CommitAsync(IBrokerMessageIdentifier brokerMessageIdentifier)
        {
            Check.NotNull(brokerMessageIdentifier, nameof(brokerMessageIdentifier));

            return CommitAsync(new[] { brokerMessageIdentifier });
        }

        /// <inheritdoc cref="IConsumer.CommitAsync(IReadOnlyCollection{IBrokerMessageIdentifier})" />
        public async Task CommitAsync(IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers)
        {
            Check.NotNull(brokerMessageIdentifiers, nameof(brokerMessageIdentifiers));

            await CommitCoreAsync(brokerMessageIdentifiers).ConfigureAwait(false);

            if (_failedAttemptsDictionary.IsEmpty)
                return;

            // TODO: Is this the most efficient way to remove a bunch of items?
            foreach (var messageIdentifier in brokerMessageIdentifiers)
            {
                _failedAttemptsDictionary.TryRemove(messageIdentifier, out _);
            }
        }

        /// <inheritdoc cref="IConsumer.RollbackAsync(IBrokerMessageIdentifier)" />
        public Task RollbackAsync(IBrokerMessageIdentifier brokerMessageIdentifier)
        {
            Check.NotNull(brokerMessageIdentifier, nameof(brokerMessageIdentifier));

            return RollbackAsync(new[] { brokerMessageIdentifier });
        }

        /// <inheritdoc cref="IConsumer.RollbackAsync(IReadOnlyCollection{IBrokerMessageIdentifier})" />
        public Task RollbackAsync(IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers)
        {
            Check.NotNull(brokerMessageIdentifiers, nameof(brokerMessageIdentifiers));

            return RollbackCoreAsync(brokerMessageIdentifiers);
        }

        /// <inheritdoc cref="IConsumer.IncrementFailedAttempts" />
        public int IncrementFailedAttempts(IRawInboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            return _failedAttemptsDictionary.AddOrUpdate(
                envelope.BrokerMessageIdentifier,
                _ => envelope.Headers.GetValueOrDefault<int>(DefaultMessageHeaders.FailedAttempts) + 1,
                (_, count) => count + 1);
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Gets the <see cref="ISequenceStore" /> instances used by this consumer. Some brokers will require
        ///     multiple stores (e.g. the <c>KafkaConsumer</c> will create a store per each assigned partition).
        /// </summary>
        /// <returns>
        ///     The list of <see cref="ISequenceStore" />.
        /// </returns>
        public IReadOnlyList<ISequenceStore> GetCurrentSequenceStores() => SequenceStores.AsReadOnlyList();

        /// <summary>
        ///     Connects to the message broker.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected abstract Task ConnectCoreAsync();

        /// <summary>
        ///     Disconnects from the message broker.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected abstract Task DisconnectCoreAsync();

        /// <summary>
        ///     Starts consuming. Called to resume consuming after <see cref="StopAsync" /> has been called.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected abstract Task StartCoreAsync();

        /// <summary>
        ///     Stops consuming while staying connected to the message broker.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected abstract Task StopCoreAsync();

        /// <summary>
        ///     Commits the specified messages sending the acknowledgement to the message broker.
        /// </summary>
        /// <param name="brokerMessageIdentifiers">
        ///     The identifiers of to message be committed.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected abstract Task CommitCoreAsync(
            IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers);

        /// <summary>
        ///     If necessary notifies the message broker that the specified messages couldn't be processed
        ///     successfully, to ensure that they will be consumed again.
        /// </summary>
        /// <param name="brokerMessageIdentifiers">
        ///     The identifiers of to message be rolled back.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected abstract Task RollbackCoreAsync(
            IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers);

        /// <summary>
        ///     Waits until the consuming is stopped.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected abstract Task WaitUntilConsumingStoppedCoreAsync();

        /// <summary>
        ///     Returns the <see cref="ISequenceStore" /> to be used to store the pending sequences.
        /// </summary>
        /// <param name="brokerMessageIdentifier">
        ///     The message identifier (the offset in Kafka) may determine which store is being used. For example a
        ///     dedicated sequence store is used per each Kafka partition, since they may be processed concurrently.
        /// </param>
        /// <returns>
        ///     The <see cref="ISequenceStore" />.
        /// </returns>
        protected virtual ISequenceStore GetSequenceStore(IBrokerMessageIdentifier brokerMessageIdentifier)
        {
            if (SequenceStores.Count == 0)
            {
                lock (SequenceStores)
                {
                    SequenceStores.Add(ServiceProvider.GetRequiredService<ISequenceStore>());
                }
            }

            return SequenceStores.FirstOrDefault() ??
                   throw new InvalidOperationException("The sequence store is not initialized.");
        }

        /// <summary>
        ///     Handles the consumed message invoking each <see cref="IConsumerBehavior" /> in the pipeline.
        /// </summary>
        /// <param name="message">
        ///     The body of the consumed message.
        /// </param>
        /// <param name="headers">
        ///     The headers of the consumed message.
        /// </param>
        /// <param name="sourceEndpointName">
        ///     The name of the actual endpoint (topic) where the message has been delivered.
        /// </param>
        /// <param name="brokerMessageIdentifier">
        ///     The identifier of the consumed message.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        [SuppressMessage("", "CA2000", Justification = "Context is disposed by the TransactionHandler")]
        protected virtual async Task HandleMessageAsync(
            byte[]? message,
            IReadOnlyCollection<MessageHeader> headers,
            string sourceEndpointName,
            IBrokerMessageIdentifier brokerMessageIdentifier)
        {
            var envelope = new RawInboundEnvelope(
                message,
                headers,
                Endpoint,
                sourceEndpointName,
                brokerMessageIdentifier);

            _statusInfo.RecordConsumedMessage(brokerMessageIdentifier);

            var consumerPipelineContext = new ConsumerPipelineContext(
                envelope,
                this,
                GetSequenceStore(brokerMessageIdentifier),
                ServiceProvider);

            await ExecutePipelineAsync(consumerPipelineContext).ConfigureAwait(false);
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
        ///     resources.
        /// </summary>
        /// <param name="disposing">
        ///     A value indicating whether the method has been called by the <c>Dispose</c> method and not from the
        ///     finalizer.
        /// </param>
        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            try
            {
                AsyncHelper.RunSynchronously(DisconnectAsync);
            }
            catch (Exception ex)
            {
                _logger.LogConsumerDisposingError(this, ex);
            }
        }

        private Task WaitUntilConsumingStoppedAsync() =>
            _logger.ExecuteAndTraceConsumerActionAsync(
                this,
                WaitUntilConsumingStoppedCoreAsync,
                "Waiting until consumer stops...",
                "Consumer stopped.");

        private Task ExecutePipelineAsync(ConsumerPipelineContext context, int stepIndex = 0)
        {
            if (_behaviors.Count == 0 || stepIndex >= _behaviors.Count)
                return Task.CompletedTask;

            return _behaviors[stepIndex].HandleAsync(
                context,
                nextContext => ExecutePipelineAsync(nextContext, stepIndex + 1));
        }
    }
}
