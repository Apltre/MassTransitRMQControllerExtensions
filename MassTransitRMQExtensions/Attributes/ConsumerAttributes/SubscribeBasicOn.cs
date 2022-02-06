using MassTransitRMQExtensions.Enums;

namespace MassTransitRMQExtensions.Attributes.ConsumerAttributes
{
    public class SubscribeBasicOn : SubscribeOn
    {
        public SubscribeBasicOn(string exchange, int concurrentMessageLimit = 1) : base(exchange, ExchangeType.Fanout, "", concurrentMessageLimit) { }
    }
}