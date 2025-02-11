﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.ArgumentResolvers;

namespace Silverback.Messaging.Messages
{
    internal sealed class LazyMessageStreamEnumerable<TMessage>
        : ILazyMessageStreamEnumerable<TMessage>,
            ILazyMessageStreamEnumerable,
            ILazyArgumentValue,
            IDisposable
    {
        private readonly TaskCompletionSource<IMessageStreamEnumerable> _taskCompletionSource =
            new();

        private MessageStreamEnumerable<TMessage>? _stream;

        public LazyMessageStreamEnumerable(IReadOnlyCollection<IMessageFilter>? filters = null)
        {
            Filters = filters;
        }

        /// <inheritdoc cref="ILazyMessageStreamEnumerable.MessageType" />
        public Type MessageType => typeof(TMessage);

        /// <inheritdoc cref="ILazyMessageStreamEnumerable.Stream" />
        public IMessageStreamEnumerable<TMessage>? Stream => _stream;

        /// <inheritdoc cref="ILazyMessageStreamEnumerable.Filters" />
        public IReadOnlyCollection<IMessageFilter>? Filters { get; }

        /// <inheritdoc cref="ILazyMessageStreamEnumerable.Stream" />
        IMessageStreamEnumerable? ILazyMessageStreamEnumerable.Stream => _stream;

        object? ILazyArgumentValue.Value => Stream;

        /// <inheritdoc cref="ILazyMessageStreamEnumerable{TMessage}.WaitUntilCreatedAsync" />
        public Task WaitUntilCreatedAsync() => _taskCompletionSource.Task;

        /// <inheritdoc cref="ILazyMessageStreamEnumerable.GetOrCreateStream" />
        public IMessageStreamEnumerable GetOrCreateStream()
        {
            if (_stream == null)
            {
                _stream = new MessageStreamEnumerable<TMessage>();
                _taskCompletionSource.SetResult(_stream);
            }

            return _stream;
        }

        /// <inheritdoc cref="ILazyMessageStreamEnumerable.Cancel" />
        public void Cancel() => _taskCompletionSource.SetCanceled();

        public void Dispose()
        {
            _stream?.Dispose();
            _stream = null;
        }
    }
}
