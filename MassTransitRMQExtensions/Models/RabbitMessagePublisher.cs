using MassTransitRMQExtensions.Enums;
using System;

namespace MassTransitRMQExtensions.Models
{
    public class RabbitMessagePublisher
    {
        public Type MessageType { get; set; }
        public string MessageExchange { get; set; }
        public ExchangeType MessageExchangeType { get; set; }
        public bool ResolveTopology { get; set; }
    }
}