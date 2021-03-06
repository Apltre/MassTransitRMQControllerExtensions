using MassTransitRMQExtensions.Enums;

namespace MassTransitRMQExtensions.Attributes.ConsumerAttributes
{
    public class SubscribeTopicOn : SubscribeOn
    {
        public SubscribeTopicOn(string exchange, string route = "#", int concurrentMessageLimit = 1) : base(exchange, ExchangeType.Topic, route, concurrentMessageLimit) { }
    }
}