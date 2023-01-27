using System;

namespace MassTransitRMQExtensions.Attributes.JobAttributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class RunJob : Attribute, IEquatable<RunJob>
    {
        public RunJob(string? cronSchedule)
        {
            CronSchedule = cronSchedule ?? "0/1 * * * * ?"; //default every second
        }
        public string CronSchedule { get; }
        public bool Equals(RunJob other)
        {

            return CronSchedule == other.CronSchedule;

        }
        public override int GetHashCode()
        {
            return CronSchedule.GetHashCode();
        }
    }
}
