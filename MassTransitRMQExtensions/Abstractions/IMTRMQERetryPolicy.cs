using MassTransitRMQExtensions.Enums;

namespace MassTransitRMQExtensions.Abstractions
{
    public interface IMTRMQERetryPolicy
    {
        RetryPolicyType RetryPolicyType { get; }
    }
}