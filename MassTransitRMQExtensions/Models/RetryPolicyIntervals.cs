using MassTransitRMQExtensions.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MassTransitRMQExtensions.Models
{
    public sealed class RetryPolicyIntervals : RetryPolicyBase
    {
        public IReadOnlyCollection<TimeSpan> Intervals { get; }
        public RetryPolicyIntervals(RetryPolicyType retryPolicyType, IEnumerable<TimeSpan> intervals) : base(retryPolicyType, RetryType.Intervals)
        {
            Intervals = intervals.ToArray() ?? throw new NullReferenceException($"Parameter {nameof(intervals)} cant be null.");
        }

        public RetryPolicyIntervals(string policy)
        {
            var (retryPolicyType, retryType, parameters) = ParsePolicyString(policy);
            RetryPolicyType = retryPolicyType;

            if (retryType != RetryType.Intervals)
            {
                throw new Exception($"Wrong policy pattern type. Expected:{RetryType.Intervals}");
            }

            RetryType = retryType;
            Intervals = parameters.Select(p => TimeSpan.FromMinutes(double.Parse(p))).ToList();
        }
    }
}
