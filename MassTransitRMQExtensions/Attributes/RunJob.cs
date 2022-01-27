using System;

namespace MassTransitRMQExtensions.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class RunJob : Attribute, IEquatable<RunJob>
    {
        public RunJob(string croneSchedule)
        {
            this.CroneSchedule = croneSchedule ?? "0/1 * * * * ?"; //default every second
        }
        public string CroneSchedule { get; }
        public bool Equals(RunJob other)
        {

            return this.CroneSchedule == other.CroneSchedule;
             
        }
        public override int GetHashCode()
        {
            return this.CroneSchedule.GetHashCode();
        }
    }
}
