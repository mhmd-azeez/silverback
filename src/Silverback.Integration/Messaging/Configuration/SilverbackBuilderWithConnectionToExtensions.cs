﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>WithConnectionToMessageBroker</c> method to the <see cref="ISilverbackBuilder" />.
    /// </summary>
    public static class SilverbackBuilderWithConnectionToExtensions
    {
        /// <summary>
        ///     Registers the types needed to connect with a message broker.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the services to.
        /// </param>
        /// <param name="optionsAction">
        ///     Additional options (such as message broker type and connectors).
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder WithConnectionToMessageBroker(
            this ISilverbackBuilder silverbackBuilder,
            Action<IBrokerOptionsBuilder>? optionsAction = null)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services
                .AddSingleton<IBrokerCollection, BrokerCollection>()
                .AddTransient(typeof(IBrokerBehaviorsProvider<>), typeof(BrokerBehaviorsProvider<>))
                .AddSingleton<ILogTemplates>(new LogTemplates())
                .AddSingleton(typeof(ISilverbackIntegrationLogger<>), typeof(SilverbackIntegrationLogger<>))
                .AddSingleton<ISilverbackIntegrationLogger, SilverbackIntegrationLogger>();

            var options = new BrokerOptionsBuilder(silverbackBuilder);
            optionsAction?.Invoke(options);
            options.CompleteWithDefaults();

            return silverbackBuilder;
        }
    }
}
