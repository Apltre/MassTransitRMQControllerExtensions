using MassTransitRMQExtensions.Enums;
using System;

namespace MassTransitRMQExtensions.Models
{
    public sealed class RetryPolicyImmediate : RetryPolicyBase
    {
        public int RetryLimit { get; }

        public RetryPolicyImmediate(RetryPolicyType retryPolicyType, int retryLimit) : base(retryPolicyType, RetryType.Immediate)
        {
            RetryLimit = retryLimit;
        }
        public RetryPolicyImmediate(string policy)
        {
            var parsedValues = ParsePolicyString(policy);
            RetryPolicyType = parsedValues.retryPolicyType;

            if (parsedValues.retryType != RetryType.Immediate)
            {
                throw new Exception($"Wrong policy pattern type. Expected:{RetryType.Immediate}");
            }

            RetryType = parsedValues.retryType;

            var parameters = parsedValues.parameters;
            if (parameters.Count > 1 || parameters.Count == 0)
            {
                throw new Exception($"Wrong parameters count {parameters.Count} for policy {parsedValues.retryPolicyType} type {parsedValues.retryType}.");
            }

            RetryLimit = int.Parse(parameters[0]);
        }
    }
}
