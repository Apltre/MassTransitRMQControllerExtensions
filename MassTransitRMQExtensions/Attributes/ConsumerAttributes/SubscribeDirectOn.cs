using MassTransitRMQExtensions.Enums;

namespace MassTransitRMQExtensions.Attributes.ConsumerAttributes
{
    public sealed class SubscribeDirectOn : SubscribeOn
    {
        public SubscribeDirectOn(string? exchange, string? route = null, int concurrentMessageLimit = 1, string? retryPolicy = null) 
            : base(exchange, ExchangeType.Direct, route, concurrentMessageLimit, retryPolicy) { }

        public SubscribeDirectOn(object? exchange, string? route = null, int concurrentMessageLimit = 1, string? retryPolicy = null)
            : base(exchange, ExchangeType.Direct, route, concurrentMessageLimit, retryPolicy) { }
    }
}