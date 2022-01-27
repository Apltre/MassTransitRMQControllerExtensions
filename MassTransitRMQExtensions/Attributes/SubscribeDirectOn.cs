using MassTransitRMQExtensions.Enums;

namespace MassTransitRMQExtensions.Attributes
{
    public class SubscribeDirectOn : SubscribeOn
    {
        public SubscribeDirectOn(string exchange, string route = "") : base(exchange, ExchangeType.Direct, route) { }
    }
}