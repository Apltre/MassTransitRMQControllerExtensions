using MassTransitRMQExtensions.Enums;

namespace MassTransitRMQExtensions.Attributes.ConsumerAttributes
{
    public class SubscribeDirectOn : SubscribeOn
    {
        public SubscribeDirectOn(string exchange, string route = "", int concurrentMessageLimit = 1) : base(exchange, ExchangeType.Direct, route, concurrentMessageLimit) { }
    }
}