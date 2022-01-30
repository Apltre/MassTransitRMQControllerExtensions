using MassTransitRMQExtensions.Enums;

namespace MassTransitRMQExtensions.Attributes.ConsumerAttributes
{
    public class SubscribeTopicOn : SubscribeOn
    {
        public SubscribeTopicOn(string exchange, string route = "#") : base(exchange, ExchangeType.Topic, route) { }
    }
}