// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers
{
    internal class SubscribedMethodsCacheSingleton
    {
        private readonly Dictionary<Type, IReadOnlyCollection<SubscribedMethod>> _cache = new();

        private readonly IBusOptions _options;

        private bool? _hasAnyMessageStreamSubscriber;

        private IReadOnlyCollection<SubscribedMethod>? _subscribedMethods;

        public SubscribedMethodsCacheSingleton(IBusOptions options)
        {
            _options = Check.NotNull(options, nameof(options));
        }

        public bool HasAnyMessageStreamSubscriber(IServiceProvider serviceProvider) =>
            _hasAnyMessageStreamSubscriber ??=
                GetAllSubscribedMethods(serviceProvider).Any(
                    method => method.MessageArgumentResolver is IStreamEnumerableMessageArgumentResolver);

        public bool IsSubscribed(object message, IServiceProvider serviceProvider) =>
            GetMethods(message, serviceProvider).Count > 0;

        public IEnumerable<SubscribedMethod> GetExclusiveMethods(
            object message,
            IServiceProvider serviceProvider) =>
            GetMethods(message, serviceProvider).Where(subscribedMethod => subscribedMethod.IsExclusive);

        public IEnumerable<SubscribedMethod> GetNonExclusiveMethods(
            object message,
            IServiceProvider serviceProvider) =>
            GetMethods(message, serviceProvider).Where(subscribedMethod => !subscribedMethod.IsExclusive);

        public IReadOnlyCollection<SubscribedMethod> GetMethods(
            object message,
            IServiceProvider serviceProvider)
        {
            var messageType = message.GetType();

            lock (_cache)
            {
                if (_cache.ContainsKey(messageType))
                    return _cache[messageType];
            }

            var subscribers = GetAllSubscribedMethods(serviceProvider)
                .Where(subscribedMethod => AreCompatible(message, subscribedMethod)).ToList();

            lock (_cache)
            {
                _cache[messageType] = subscribers;
            }

            return subscribers;
        }

        public void Preload(IServiceProvider serviceProvider) => GetAllSubscribedMethods(serviceProvider);

        private static bool AreCompatible(object message, SubscribedMethod subscribedMethod)
        {
            if (subscribedMethod.MessageArgumentResolver is IStreamEnumerableMessageArgumentResolver)
                return AreCompatibleStreams(message, subscribedMethod);

            if (message is IEnvelope { AutoUnwrap: true } envelope &&
                subscribedMethod.MessageType.IsInstanceOfType(envelope.Message))
                return true;

            if (subscribedMethod.MessageType.IsInstanceOfType(message))
                return true;

            return false;
        }

        private static bool AreCompatibleStreams(object message, SubscribedMethod subscribedMethod)
        {
            if (message is not IMessageStreamProvider streamProvider)
                return false;

            // There is no way to properly match the message types in the case of a stream of IEnvelope
            // and a subscriber that is not handling a stream of envelopes. The envelopes can contain any
            // type of message (object? Message) and will automatically be unwrapped, filtered and properly
            // routed by the MessageStreamEnumerable.
            if (typeof(IEnvelope).IsAssignableFrom(streamProvider.MessageType) &&
                !typeof(IEnvelope).IsAssignableFrom(subscribedMethod.MessageType))
                return true;

            if (streamProvider.MessageType.IsAssignableFrom(subscribedMethod.MessageType) ||
                subscribedMethod.MessageType.IsAssignableFrom(streamProvider.MessageType))
                return true;

            return false;
        }

        private IReadOnlyCollection<SubscribedMethod> GetAllSubscribedMethods(
            IServiceProvider serviceProvider) =>
            _subscribedMethods ??= LoadSubscribedMethods(serviceProvider);

        private List<SubscribedMethod> LoadSubscribedMethods(IServiceProvider serviceProvider) =>
            _options.Subscriptions
                .SelectMany(subscription => subscription.GetSubscribedMethods(serviceProvider))
                .ToList();
    }
}
