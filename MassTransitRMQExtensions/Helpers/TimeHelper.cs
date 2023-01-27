using System;
using System.Collections.Generic;
using System.Linq;

namespace MassTransitRMQExtensions.Helpers
{
    internal static class TimeHelper
    {
        private static int ceil(double value)
        {
            var integerPart = (int)value;
            return integerPart + (value - integerPart > 0 ? 1 : 0);
        }
        internal static IReadOnlyList<int> GetExponentialIntervalsSeconds(TimeSpan minInterval, TimeSpan maxInterval, int? maxIntervalsNumber = null)
        {
            var minIntervalSeconds = ceil(minInterval.TotalSeconds);
            var maxIntervalSeconds = (int)maxInterval.TotalSeconds;

            var exponentialSequenceBeginningNumber = ceil(Math.Log(minIntervalSeconds));
            var exponentialSequenceEndNumber = (int)(Math.Log(maxIntervalSeconds));

            if (exponentialSequenceBeginningNumber > exponentialSequenceEndNumber)
            {
                return new List<int> { minIntervalSeconds };
            }

            var intervals = new List<int>();

            intervals.Add(minIntervalSeconds);

            for (int i = exponentialSequenceBeginningNumber; i <= exponentialSequenceEndNumber; i++)
            {
                intervals.Add((int)Math.Exp(i));
            }
            intervals.Add(maxIntervalSeconds);

            if (maxIntervalsNumber.HasValue && intervals.Count > maxIntervalsNumber)
            {
                intervals = intervals.Take(maxIntervalsNumber.Value).ToList();
            }

            return intervals;
        }
    }
}