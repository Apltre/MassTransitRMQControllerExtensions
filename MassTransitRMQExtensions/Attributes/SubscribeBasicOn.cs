using MassTransitRMQExtensions.Enums;

namespace MassTransitRMQExtensions.Attributes
{
    public class SubscribeBasicOn : SubscribeOn
    {
        public SubscribeBasicOn(string exchange) : base(exchange, ExchangeType.Fanout, "") { }
    }
}