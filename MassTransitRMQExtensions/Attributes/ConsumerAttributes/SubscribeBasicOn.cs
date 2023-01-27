using MassTransitRMQExtensions.Enums;

namespace MassTransitRMQExtensions.Attributes.ConsumerAttributes
{
    public sealed class SubscribeBasicOn : SubscribeOn
    {
        public SubscribeBasicOn(string exchange, int concurrentMessageLimit = 1, string? retryPolicy = null) 
            : base(exchange, ExchangeType.Fanout, null, concurrentMessageLimit, retryPolicy) { }

        public SubscribeBasicOn(object exchange, int concurrentMessageLimit = 1, string? retryPolicy = null)
            : base(exchange, ExchangeType.Fanout, null, concurrentMessageLimit, retryPolicy) { }
    }
}