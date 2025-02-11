﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka.Mocks;

namespace Silverback.Testing
{
    /// <inheritdoc cref="ITestingHelper{TBroker}" />
    public interface IKafkaTestingHelper : ITestingHelper<KafkaBroker>
    {
        /// <summary>
        ///     Returns the <see cref="IInMemoryTopic" /> with the specified name.
        /// </summary>
        /// <remarks>
        ///     This method works with the mocked MQTT broker only. See
        ///     <see cref="SilverbackBuilderUseMockedKafkaExtensions.UseMockedKafka" /> or
        ///     <see cref="BrokerOptionsBuilderAddMockedKafkaExtensions.AddMockedKafka" />.
        /// </remarks>
        /// <param name="name">
        ///     The name of the topic.
        /// </param>
        /// <returns>
        ///     The <see cref="IInMemoryTopic" />.
        /// </returns>
        IInMemoryTopic GetTopic(string name);

        /// <summary>
        ///     Returns a <see cref="Task" /> that completes when all messages routed to the consumers have been
        ///     processed and committed.
        /// </summary>
        /// <remarks>
        ///     This method works with the mocked MQTT broker only. See
        ///     <see cref="SilverbackBuilderUseMockedKafkaExtensions.UseMockedKafka" /> or
        ///     <see cref="BrokerOptionsBuilderAddMockedKafkaExtensions.AddMockedKafka" />.
        /// </remarks>
        /// <param name="topicNames">
        ///     The name of the topics to be monitored.
        /// </param>
        /// <param name="timeout">
        ///     The time to wait for the messages to be consumed and processed. The default is 30 seconds.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> that completes when all messages have been processed.
        /// </returns>
        Task WaitUntilAllMessagesAreConsumedAsync(
            IReadOnlyCollection<string> topicNames,
            TimeSpan? timeout = null);
    }
}
