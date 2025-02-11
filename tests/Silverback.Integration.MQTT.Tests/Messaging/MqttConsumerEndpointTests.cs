﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using MQTTnet.Client.Options;
using MQTTnet.Formatter;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging
{
    public class MqttConsumerEndpointTests
    {
        [Fact]
        public void Equals_SameEndpointInstance_TrueIsReturned()
        {
            var endpoint = new MqttConsumerEndpoint("topic")
            {
                Configuration =
                {
                    ClientId = "client1"
                }
            };

            endpoint.Equals(endpoint).Should().BeTrue();
        }

        [Fact]
        public void Equals_SameConfiguration_TrueIsReturned()
        {
            var endpoint1 = new MqttConsumerEndpoint("topic")
            {
                Configuration =
                {
                    ClientId = "client1"
                }
            };

            var endpoint2 = new MqttConsumerEndpoint("topic")
            {
                Configuration =
                {
                    ClientId = "client1"
                }
            };

            endpoint1.Equals(endpoint2).Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentTopic_FalseIsReturned()
        {
            var endpoint1 = new MqttConsumerEndpoint("topic")
            {
                Configuration =
                {
                    ClientId = "client1"
                }
            };

            var endpoint2 = new MqttConsumerEndpoint("topic2")
            {
                Configuration =
                {
                    ClientId = "client1"
                }
            };

            endpoint1.Equals(endpoint2).Should().BeFalse();
        }

        [Fact]
        public void Equals_DifferentConfiguration_FalseIsReturned()
        {
            var endpoint1 = new MqttConsumerEndpoint("topic")
            {
                Configuration =
                {
                    ClientId = "client1"
                }
            };

            var endpoint2 = new MqttConsumerEndpoint("topic")
            {
                Configuration =
                {
                    ClientId = "client2"
                }
            };

            endpoint1.Equals(endpoint2).Should().BeFalse();
        }

        [Fact]
        public void Validate_ValidTopicAndConfiguration_NoExceptionThrown()
        {
            var endpoint = GetValidEndpoint();

            Action act = () => endpoint.Validate();

            act.Should().NotThrow<EndpointConfigurationException>();
        }

        [Fact]
        public void Validate_MissingConfiguration_ExceptionThrown()
        {
            var endpoint = new MqttConsumerEndpoint("topic")
            {
                Configuration = null!
            };

            Action act = () => endpoint.Validate();

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Fact]
        public void Validate_InvalidConfiguration_ExceptionThrown()
        {
            var endpoint = new MqttConsumerEndpoint("topic")
            {
                Configuration = new MqttClientConfig()
            };

            Action act = () => endpoint.Validate();

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Fact]
        public void Validate_MissingTopic_ExceptionThrown()
        {
            var endpoint = new MqttConsumerEndpoint
            {
                Configuration = new MqttClientConfig
                {
                    ChannelOptions = new MqttClientTcpOptions
                    {
                        Server = "test-server"
                    }
                }
            };

            Action act = () => endpoint.Validate();

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Fact]
        public void Validate_RetryPolicyWithMultipleRetriesOnV311_ExceptionThrown()
        {
            var endpoint = new MqttConsumerEndpoint("topic")
            {
                Configuration = new MqttClientConfig
                {
                    ChannelOptions = new MqttClientTcpOptions
                    {
                        Server = "test-server"
                    },
                    ProtocolVersion = MqttProtocolVersion.V311
                },
                ErrorPolicy = new RetryErrorPolicy().MaxFailedAttempts(10)
            };

            Action act = () => endpoint.Validate();

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Fact]
        public void Validate_ChainedErrorPoliciesOnV311_ExceptionThrown()
        {
            var endpoint = new MqttConsumerEndpoint("topic")
            {
                Configuration = new MqttClientConfig
                {
                    ChannelOptions = new MqttClientTcpOptions
                    {
                        Server = "test-server"
                    },
                    ProtocolVersion = MqttProtocolVersion.V311
                },
                ErrorPolicy = new ErrorPolicyChain(new RetryErrorPolicy(), new SkipMessageErrorPolicy())
            };

            Action act = () => endpoint.Validate();

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Fact]
        public void Validate_DynamicTypeSerializerOnV311_ExceptionThrown()
        {
            var endpoint = new MqttConsumerEndpoint("topic")
            {
                Configuration = new MqttClientConfig
                {
                    ChannelOptions = new MqttClientTcpOptions
                    {
                        Server = "test-server"
                    },
                    ProtocolVersion = MqttProtocolVersion.V311
                },
                Serializer = new JsonMessageSerializer()
            };

            Action act = () => endpoint.Validate();

            act.Should().ThrowExactly<EndpointConfigurationException>();
        }

        [Fact]
        public void Validate_FixedTypeSerializerOnV311_NoExceptionThrown()
        {
            var endpoint = new MqttConsumerEndpoint("topic")
            {
                Configuration = new MqttClientConfig
                {
                    ChannelOptions = new MqttClientTcpOptions
                    {
                        Server = "test-server"
                    },
                    ProtocolVersion = MqttProtocolVersion.V311
                },
                Serializer = new JsonMessageSerializer<TestEventOne>()
            };

            Action act = () => endpoint.Validate();

            act.Should().NotThrow();
        }

        [Fact]
        public void Validate_RetryPolicyWithMultipleRetriesOnV500_NoExceptionThrown()
        {
            var endpoint = new MqttConsumerEndpoint("topic")
            {
                Configuration = new MqttClientConfig
                {
                    ChannelOptions = new MqttClientTcpOptions
                    {
                        Server = "test-server"
                    },
                    ProtocolVersion = MqttProtocolVersion.V500
                },
                ErrorPolicy = new RetryErrorPolicy().MaxFailedAttempts(10)
            };

            Action act = () => endpoint.Validate();

            act.Should().NotThrow();
        }

        [Fact]
        public void Validate_ChainedErrorPoliciesOnDefaultV500_NoExceptionThrown()
        {
            var endpoint = new MqttConsumerEndpoint("topic")
            {
                Configuration = new MqttClientConfig
                {
                    ChannelOptions = new MqttClientTcpOptions
                    {
                        Server = "test-server"
                    },
                    ProtocolVersion = MqttProtocolVersion.V500
                },
                ErrorPolicy = new ErrorPolicyChain(new RetryErrorPolicy(), new SkipMessageErrorPolicy())
            };

            Action act = () => endpoint.Validate();

            act.Should().NotThrow();
        }

        [Fact]
        public void Validate_DynamicTypeSerializerOnV500_NoExceptionThrown()
        {
            var endpoint = new MqttConsumerEndpoint("topic")
            {
                Configuration = new MqttClientConfig
                {
                    ChannelOptions = new MqttClientTcpOptions
                    {
                        Server = "test-server"
                    },
                    ProtocolVersion = MqttProtocolVersion.V500
                },
                Serializer = new JsonMessageSerializer()
            };

            Action act = () => endpoint.Validate();

            act.Should().NotThrow();
        }

        private static MqttConsumerEndpoint GetValidEndpoint() =>
            new("test")
            {
                Configuration = new MqttClientConfig
                {
                    ChannelOptions = new MqttClientTcpOptions
                    {
                        Server = "test-server"
                    }
                }
            };
    }
}
