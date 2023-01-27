using MassTransitRMQExtensions.Enums;
using System;

namespace MassTransitRMQExtensions.Models
{
    public class RabbitMessagePublisher
    {
        public RabbitMessagePublisher(Type messageType, string messageExchange, ExchangeType messageExchangeType, bool resolveTopology)
        {
            MessageType = messageType;
            MessageExchange = messageExchange;
            MessageExchangeType = messageExchangeType;
            ResolveTopology = resolveTopology;
        }

        public Type MessageType { get; set; }
        public string MessageExchange { get; set; }
        public ExchangeType MessageExchangeType { get; set; }
        public bool ResolveTopology { get; set; }
    }
}