using MassTransitRMQExtensions.Enums;

namespace MassTransitRMQExtensions.Attributes.ConsumerAttributes
{
    public class SubscribeEndpoint : SubscribeOn
    {
        public SubscribeEndpoint(int concurrentMessageLimit = 1, string? retryPolicy = null) 
            : base(null, ExchangeType.None, concurrentMessageLimit: concurrentMessageLimit, retryPolicy: retryPolicy)
        { }
    }
}
