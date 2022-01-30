using MassTransitRMQExtensions.Enums;

namespace MassTransitRMQExtensions.Attributes.ConsumerAttributes
{
    public class SubscribeDirectOn : SubscribeOn
    {
        public SubscribeDirectOn(string exchange, string route = "") : base(exchange, ExchangeType.Direct, route) { }
    }
}