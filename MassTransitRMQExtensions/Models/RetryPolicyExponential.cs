using MassTransitRMQExtensions.Enums;
using MassTransitRMQExtensions.Helpers;
using System;
using System.Collections.Generic;

namespace MassTransitRMQExtensions.Models
{
    public sealed class RetryPolicyExponential : RetryPolicyBase
    {
        public int RetryLimit { get; private set; }
        public TimeSpan MinInterval { get; }
        public TimeSpan MaxInterval { get; }
        public TimeSpan IntervalDelta { get; }
        public IReadOnlyList<int> RetryDelaysInSeconds { get; }

        public RetryPolicyExponential(RetryPolicyType retryPolicyType, int retryLimit, TimeSpan minInterval, TimeSpan maxInterval, TimeSpan? intervalDelta = null) : base(retryPolicyType, RetryType.Exponential)
        {
            RetryLimit = retryLimit;
            if (minInterval > maxInterval)
            {
                throw new Exception($"Retry policy {nameof(RetryPolicyExponential)} minInterval is more than maxInterval");
            }
            MinInterval = minInterval;
            MaxInterval = maxInterval;

            IntervalDelta = intervalDelta ?? TimeSpan.Zero;
            RetryDelaysInSeconds = TimeHelper.GetExponentialIntervalsSeconds(minInterval, maxInterval, retryLimit);
        }

        public RetryPolicyExponential(string policy)
        {
            var parsedValues = ParsePolicyString(policy);
            RetryPolicyType = parsedValues.retryPolicyType;

            if (parsedValues.retryType != RetryType.Exponential)
            {
                throw new Exception($"Wrong policy pattern type. Expected:{RetryType.Exponential}");
            }
            RetryType = parsedValues.retryType;

            var parameters = parsedValues.parameters;

            if (parameters.Count < 4)
            {
                parameters.Add("0");
            }

            if (parameters.Count != 4)
            {
                throw new Exception($"Wrong parameters count {parameters.Count} for policy {parsedValues.retryPolicyType} type {parsedValues.retryType}.");
            }
            RetryLimit = int.Parse(parameters[0]);
            MinInterval = TimeSpan.FromMinutes(double.Parse(parameters[1]));
            MaxInterval = TimeSpan.FromMinutes(double.Parse(parameters[2]));
            IntervalDelta = TimeSpan.FromMinutes(double.Parse(parameters[3]));
            RetryDelaysInSeconds = TimeHelper.GetExponentialIntervalsSeconds(MinInterval, MaxInterval, RetryLimit);
        }
    }
}
