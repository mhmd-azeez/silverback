﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Sequences
{
    /// <summary>
    ///     Represents a set of logically related messages, like the chunks belonging to the same message or the
    ///     messages in a dataset.
    /// </summary>
    public interface ISequence : IDisposable
    {
        /// <summary>
        ///     Gets the identifier that is used to match the consumed messages with their belonging sequence.
        /// </summary>
        object SequenceId { get; }

        /// <summary>
        ///     Gets a value indicating whether the first message in the sequence was consumed and this instance was
        ///     just created.
        /// </summary>
        bool IsNew { get; }

        /// <summary>
        ///     Gets the length of the sequence so far.
        /// </summary>
        int Length { get; }

        /// <summary>
        ///     Gets the declared total length of the sequence, if known.
        /// </summary>
        int? TotalLength { get; }

        /// <summary>
        ///     Gets a value indicating whether the sequence is incomplete and awaiting for new messages to be pushed.
        /// </summary>
        bool IsPending { get; }

        /// <summary>
        ///     Gets a value indicating whether all messages belonging to the sequence have been pushed.
        /// </summary>
        bool IsComplete { get; }

        /// <summary>
        ///     Gets a value indicating whether the sequence processing has been aborted and no further message will
        ///     be pushed.
        /// </summary>
        bool IsAborted { get; }

        /// <summary>
        ///     Gets the offsets of the messages belonging to the sequence.
        /// </summary>
        IReadOnlyList<IOffset> Offsets { get; }

        /// <summary>
        ///     Gets a stream that will be pushed with the messages belonging to the sequence.
        /// </summary>
        IMessageStreamEnumerable<IRawInboundEnvelope> Stream { get; }

        /// <summary>
        ///     Gets the <see cref="ConsumerPipelineContext" /> related to the processing of this sequence
        ///     (usually the context of the first message that initiated the sequence).
        /// </summary>
        ConsumerPipelineContext Context { get; }

        /// <summary>
        ///     Adds the message to the sequence.
        /// </summary>
        /// <param name="envelope">
        ///     The envelope to be added to the sequence.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task AddAsync(IRawInboundEnvelope envelope);

        /// <summary>
        ///     Aborts the sequence processing. Used for example to signal that an exception occurred or the
        ///     enumeration returned prematurely.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task AbortAsync();
    }
}
