using MassTransitRMQExtensions.Abstractions;
using MassTransitRMQExtensions.Enums;
using System;

namespace MassTransitRMQExtensions.Models
{
    public sealed class RetryPolicyNone : IMTRMQERetryPolicy
    {
        public RetryPolicyType RetryPolicyType => RetryPolicyType.None;
        public RetryPolicyNone() 
        {
        }

        public RetryPolicyNone(string policy)
        {
            policy = policy.ToLower();
            if (policy != "n" && policy != "none")
            {
                throw new Exception($"Wrong policy pattern for {RetryPolicyType.None}");
            }
        }
    }
}