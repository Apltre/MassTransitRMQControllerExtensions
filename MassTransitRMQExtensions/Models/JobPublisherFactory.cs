using Quartz;
using Quartz.Spi;
using System;

namespace MassTransitRMQExtensions.Models
{
    internal class JobPublisherFactory : IJobFactory
    {
        public JobPublisherFactory(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            return new JobPublisher(this.ServiceProvider);
        }

        public void ReturnJob(IJob job)
        {
            if (job is IDisposable disposableJob)
                disposableJob.Dispose();
        }
    }
}