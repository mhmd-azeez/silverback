// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.E2E.TestTypes
{
    public class OutboundInboundSubscriber
    {
        private readonly List<IOutboundEnvelope> _outboundEnvelopes = new List<IOutboundEnvelope>();

        private readonly List<IInboundEnvelope> _inboundEnvelopes = new List<IInboundEnvelope>();

        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "Lock on writes only")]
        public IReadOnlyList<IOutboundEnvelope> OutboundEnvelopes => _outboundEnvelopes.ToList();

        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "Lock on writes only")]
        public IReadOnlyList<IInboundEnvelope> InboundEnvelopes => _inboundEnvelopes.ToList();

        public void OnOutbound(IOutboundEnvelope envelope)
        {
            lock (_outboundEnvelopes)
            {
                _outboundEnvelopes.Add(envelope);
            }
        }

        public void OnInbound(IInboundEnvelope envelope)
        {
            lock (_inboundEnvelopes)
            {
                _inboundEnvelopes.Add(envelope);
            }
        }
    }
}
