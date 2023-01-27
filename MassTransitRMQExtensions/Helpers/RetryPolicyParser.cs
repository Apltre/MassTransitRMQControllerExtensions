using MassTransitRMQExtensions.Abstractions;
using MassTransitRMQExtensions.Enums;
using MassTransitRMQExtensions.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MassTransitRMQExtensions.Helpers
{
    public static class RetryPolicyParser
    {
        internal static IReadOnlyDictionary<RetryType, Func<string, IMTRMQERetryPolicy>> PolicyCreators = new Dictionary<RetryType, Func<string, IMTRMQERetryPolicy>>()
        {
            { RetryType.Immediate, (p) => new RetryPolicyImmediate(p)},
            { RetryType.Interval, (p) => new RetryPolicyInterval(p)},
            { RetryType.Intervals, (p) => new RetryPolicyIntervals(p)},
            { RetryType.Exponential, (p) => new RetryPolicyExponential(p)},
            { RetryType.Incremental, (p) => new RetryPolicyIncremental(p)}
        };
        public static IMTRMQERetryPolicy Parse(string policy)
        {
            var parameters = policy.NormalizePolicyPattern().Split(' ').ToArray();

            if (parameters.Length == 1)
            {
                return new RetryPolicyNone(parameters[0]);
            }

            if (!Enum.TryParse(parameters[1], true, out RetryType retryType))
            {
                throw new Exception($"Wrong retry policy type: {parameters[1]}!");
            }

            return PolicyCreators[retryType](policy);
        }

        public static string NormalizePolicyPattern(this string policy)
        {
            return $" {policy.ToLower()} "
            .Replace(".",",")
            .Replace(" n ", " none ")
            .Replace(" mt ", " masstransit ")
            .Replace(" rmq ", " rabbitmq ")
            .Replace(" imm ", " immediate ")
            .Replace(" exp ", " exponential ")
            .Replace(" inc ", " incremental ")
            .Replace(" intvl ", " interval ")
            .Replace(" intvls ", " intervals ")
            .Trim();
        }
    }
}
