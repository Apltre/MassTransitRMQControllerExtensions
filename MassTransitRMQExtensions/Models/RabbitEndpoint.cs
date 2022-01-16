using System;
using MassTransitRMQExtensions.Enums;

namespace MassTransitRMQExtensions.Models
{
    public class RabbitEndpoint
    {
        public string ExchangeName { get; set; }
        public string QueueName { get; set; }
        public string TopicRoutingKey { get; set; } = String.Empty;
        public Type ConsumerMessageType { get; set; }
        public ExchangeType ExchangeType { get; set; }
        public ControllerHandlerInfo EventHandler { get; set;}
    }
}
