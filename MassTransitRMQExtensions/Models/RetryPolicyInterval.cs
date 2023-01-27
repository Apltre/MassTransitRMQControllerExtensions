using MassTransitRMQExtensions.Enums;
using System;

namespace MassTransitRMQExtensions.Models
{
    public sealed class RetryPolicyInterval : RetryPolicyBase
    {
        public int RetryLimit { get; }
        public TimeSpan Interval { get; }
        public RetryPolicyInterval(RetryPolicyType retryPolicyType, int retryLimit, TimeSpan interval) : base(retryPolicyType, RetryType.Interval)
        {
            RetryLimit = retryLimit;
            Interval = interval;
        }

        public RetryPolicyInterval(string policy)
        {
            var parsedValues = ParsePolicyString(policy);
            RetryPolicyType = parsedValues.retryPolicyType;

            if (parsedValues.retryType != RetryType.Interval)
            {
                throw new Exception($"Wrong policy pattern type. Expected:{RetryType.Interval}");
            }

            RetryType = parsedValues.retryType;

            var parameters = parsedValues.parameters;

            if (parameters.Count > 2 || parameters.Count == 0)
            {
                throw new Exception($"Wrong parameters count {parameters.Count} for policy {parsedValues.retryPolicyType} type {parsedValues.retryType}.");
            }

            RetryLimit = int.Parse(parameters[0]);
            Interval = TimeSpan.FromMinutes(Convert.ToDouble(parameters[1]));
        }
    }
}
