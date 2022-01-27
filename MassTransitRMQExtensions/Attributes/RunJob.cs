using System;

namespace MassTransitRMQExtensions.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class RunJob : Attribute, IEquatable<RunJob>
    {
        public RunJob(string cronSchedule)
        {
            this.CronSchedule = cronSchedule ?? "0/1 * * * * ?"; //default every second
        }
        public string CronSchedule { get; }
        public bool Equals(RunJob other)
        {

            return this.CronSchedule == other.CronSchedule;
             
        }
        public override int GetHashCode()
        {
            return this.CronSchedule.GetHashCode();
        }
    }
}
