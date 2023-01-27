using MassTransitRMQExtensions.Enums;
using System;

namespace MassTransitRMQExtensions.Models
{
    public sealed class RetryPolicyIncremental : RetryPolicyBase
    {
        public int RetryLimit { get; }
        public TimeSpan InitialInterval { get; }
        public TimeSpan IntervalIncrement { get; }
        public RetryPolicyIncremental(RetryPolicyType retryPolicyType, int retryLimit, TimeSpan initialInterval, TimeSpan intervalIncrement) : base(retryPolicyType, RetryType.Incremental)
        {
            RetryLimit = retryLimit;
            InitialInterval = initialInterval;
            IntervalIncrement = intervalIncrement;
        }

        public RetryPolicyIncremental(string policy)
        {
            var parsedValues = ParsePolicyString(policy);
            RetryPolicyType = parsedValues.retryPolicyType;

            if (parsedValues.retryType != RetryType.Incremental)
            {
                throw new Exception($"Wrong policy pattern type. Expected:{RetryType.Incremental}");
            }

            RetryType = parsedValues.retryType;

            var parameters = parsedValues.parameters;
            if (parameters.Count > 3 || parameters.Count == 0)
            {
                throw new Exception($"Wrong parameters count {parameters.Count} for policy {parsedValues.retryPolicyType} type {parsedValues.retryType}.");
            }
            RetryLimit = int.Parse(parameters[0]);
            InitialInterval = TimeSpan.FromMinutes(double.Parse(parameters[1]));
            IntervalIncrement = TimeSpan.FromMinutes(double.Parse(parameters[2]));
        }
    }
}
