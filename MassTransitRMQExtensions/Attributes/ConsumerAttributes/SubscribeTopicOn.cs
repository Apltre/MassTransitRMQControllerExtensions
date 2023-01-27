using MassTransitRMQExtensions.Enums;

namespace MassTransitRMQExtensions.Attributes.ConsumerAttributes
{
    public sealed class SubscribeTopicOn : SubscribeOn
    {
        public SubscribeTopicOn(string? exchange, string route = "#", int concurrentMessageLimit = 1, string? retryPolicy = null)
            : base(exchange, ExchangeType.Topic, route, concurrentMessageLimit, retryPolicy) { }

        public SubscribeTopicOn(object? exchange, string route = "#", int concurrentMessageLimit = 1, string? retryPolicy = null)
            : base(exchange, ExchangeType.Topic, route, concurrentMessageLimit, retryPolicy) { }
    }
}