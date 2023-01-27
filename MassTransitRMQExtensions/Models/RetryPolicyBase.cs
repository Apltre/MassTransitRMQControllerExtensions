using MassTransitRMQExtensions.Abstractions;
using MassTransitRMQExtensions.Enums;
using MassTransitRMQExtensions.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MassTransitRMQExtensions.Models
{
    public abstract class RetryPolicyBase : IMTRMQERetryPolicy
    {
        public RetryPolicyType RetryPolicyType { get; protected set; }
        public RetryType RetryType { get; protected set; }

        protected RetryPolicyBase() { }

        protected RetryPolicyBase(RetryPolicyType retryPolicyType, RetryType retryType)
        {
            RetryPolicyType = retryPolicyType;
            RetryType = retryType;
        }

        protected static (RetryPolicyType retryPolicyType, RetryType retryType, List<string> parameters) ParsePolicyString(string policy)
        {
            policy = policy.NormalizePolicyPattern();

            if (policy is null)
            {
                throw new ArgumentNullException(nameof(policy));
            }
            var parameters = policy.Split(' ').ToArray();

            if (parameters.Length < 2)
            {
                throw new Exception("Wrong parameters number!");
            }

            if (!Enum.TryParse(parameters[0], true, out RetryPolicyType retryPolicyType))
            {
                throw new Exception($"Wrong retry policy configurator type: {parameters[0]}!");
            }

            if (!Enum.TryParse(parameters[1], true, out RetryType retryType))
            {
                throw new Exception($"Wrong retry policy type: {parameters[1]}!");
            }

            var retryFunctionParameters = parameters.Skip(2).ToList();
            return (retryPolicyType, retryType, retryFunctionParameters);
        }
    }
}
