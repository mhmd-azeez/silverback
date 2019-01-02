﻿// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Connectors
{
    [TestFixture]
    public class InboundConnectorTests
    {
        private TestSubscriber _testSubscriber;
        private IInboundConnector _connector;
        private TestBroker _broker;
        private ErrorPolicyBuilder _errorPolicyBuilder;

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();

            _testSubscriber = new TestSubscriber();
            services.AddSingleton<ISubscriber>(_testSubscriber);

            services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
            services.AddSingleton<IPublisher, Publisher>();

            _broker = new TestBroker();
            services.AddSingleton<IBroker>(_broker);

            var serviceProvider = services.BuildServiceProvider();
            _connector = new InboundConnector(_broker, serviceProvider, new NullLogger<InboundConnector>());
            _errorPolicyBuilder = new ErrorPolicyBuilder(serviceProvider, NullLoggerFactory.Instance);
        }

        [Test]
        public void Bind_PushMessages_MessagesReceived()
        {
            _connector.Bind(TestEndpoint.Default);
            _broker.Connect();

            var consumer = (TestConsumer)_broker.GetConsumer(TestEndpoint.Default);
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

            Assert.That(_testSubscriber.ReceivedMessages.Count, Is.EqualTo(5));
        }
        
        [Test]
        public void Bind_PushMessagesInBatch_MessagesReceived()
        {
            var endpoint = new TestEndpoint("test")
            {
                Batch = new Silverback.Messaging.Batch.BatchSettings
                {
                    Size = 5
                }
            };

            _connector.Bind(endpoint);
            _broker.Connect();

            var consumer = (TestConsumer)_broker.GetConsumer(endpoint);
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

            Assert.That(_testSubscriber.ReceivedMessages.Count, Is.EqualTo(0));

            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

            Assert.That(_testSubscriber.ReceivedMessages.Count, Is.EqualTo(7));
        }

        [Test]
        public void Bind_PushMessagesInMultipleBatches_MessagesReceived()
        {
            var endpoint = new TestEndpoint("test")
            {
                Batch = new Silverback.Messaging.Batch.BatchSettings
                {
                    Size = 5
                }
            };

            _connector.Bind(endpoint);
            _broker.Connect();

            var consumer = (TestConsumer)_broker.GetConsumer(endpoint);
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });

            Assert.That(_testSubscriber.ReceivedMessages.Count, Is.EqualTo(7));
            _testSubscriber.ReceivedMessages.Clear();

            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventTwo { Id = Guid.NewGuid() });
            consumer.TestPush(new TestEventOne { Id = Guid.NewGuid() });

            Assert.That(_testSubscriber.ReceivedMessages.Count, Is.EqualTo(7));
        }

        [Test]
        public void Bind_WithRetryErrorPolicy_RetriedAndReceived()
        {
            _testSubscriber.MustFailCount = 3;
            _connector.Bind(TestEndpoint.Default, _errorPolicyBuilder.Retry().MaxFailedAttempts(3));
            _broker.Connect();

            var consumer = (TestConsumer)_broker.GetConsumer(TestEndpoint.Default);
            consumer.TestPush(new TestEventOne { Content = "Test", Id = Guid.NewGuid() });

            Assert.That(_testSubscriber.FailCount, Is.EqualTo(3));
            Assert.That(_testSubscriber.ReceivedMessages.Count, Is.EqualTo(1));
        }

        [Test]
        public void Bind_WithChainedErrorPolicy_RetriedAndMoved()
        {
            _testSubscriber.MustFailCount = 3;
            _connector.Bind(TestEndpoint.Default, _errorPolicyBuilder.Chain(
                _errorPolicyBuilder.Retry().MaxFailedAttempts(1),
                _errorPolicyBuilder.Move(new TestEndpoint("bad"))));
            _broker.Connect();

            var consumer = (TestConsumer)_broker.GetConsumer(TestEndpoint.Default);
            consumer.TestPush(new TestEventOne { Content = "Test", Id = Guid.NewGuid() });

            var producer = (TestProducer)_broker.GetProducer(new TestEndpoint("bad"));

            Assert.That(_testSubscriber.FailCount, Is.EqualTo(2));
            Assert.That(producer.ProducedMessages.Count, Is.EqualTo(1));
            Assert.That(_testSubscriber.ReceivedMessages.Count, Is.EqualTo(0));
        }
        
        [Test]
        public void Bind_WithRetryErrorPolicy_RetriedAndReceivedInBatch()
        {
            var endpoint = new TestEndpoint("test")
            {
                Batch = new Silverback.Messaging.Batch.BatchSettings
                {
                    Size = 2
                }
            };

            _testSubscriber.MustFailCount = 3;
            _connector.Bind(TestEndpoint.Default, _errorPolicyBuilder.Retry().MaxFailedAttempts(3));
            _broker.Connect();

            var consumer = (TestConsumer)_broker.GetConsumer(TestEndpoint.Default);
            consumer.TestPush(new TestEventOne { Content = "Test", Id = Guid.NewGuid() });

            Assert.That(_testSubscriber.FailCount, Is.EqualTo(3));
            Assert.That(_testSubscriber.ReceivedMessages.Count, Is.EqualTo(1));
        }
    }
}