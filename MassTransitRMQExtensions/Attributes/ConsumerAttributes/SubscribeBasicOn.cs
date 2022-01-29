using MassTransitRMQExtensions.Enums;

namespace MassTransitRMQExtensions.Attributes.ConsumerAttributes
{
    public class SubscribeBasicOn : SubscribeOn
    {
        public SubscribeBasicOn(string exchange) : base(exchange, ExchangeType.Fanout, "") { }
    }
}