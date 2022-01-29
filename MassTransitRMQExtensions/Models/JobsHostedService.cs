using MassTransit;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Impl;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using MassTransitRMQExtensions.Helpers;
using MassTransitRMQExtensions.Attributes.JobAttributes;

namespace MassTransitRMQExtensions.Models
{
    internal class JobsHostedService : IHostedService
    {
        public JobsHostedService(IServiceProvider serviceProvider, IEnumerable<Type> controllers, Func<string, string> queueNamingChanger = null)
        {
            this.ServiceProvider = serviceProvider;
            this.Controllers = controllers;
            if (queueNamingChanger is null)
            {
                queueNamingChanger = (n) => n;
            }
            this.QueueNamingChanger = queueNamingChanger ;
        }
        public IServiceProvider ServiceProvider { get; }
        public IEnumerable<Type> Controllers { get; }
        public Func<string, string> QueueNamingChanger { get; }
        public async Task StartAsync(CancellationToken cancellationToken)
        {   
            var methodsToBind = this.Controllers.SelectMany(t => t.GetMethods()).Where(m => m.CheckMethodHasAttribute<RunJob>()).ToList();

            if (methodsToBind.Any())
            {
                var scheduleFactory = new StdSchedulerFactory();
                var scheduler = await StdSchedulerFactory.GetDefaultScheduler();
                await scheduler.Start();
                foreach (var method in methodsToBind)
                {
                    var queueName = this.QueueNamingChanger(method.GetQueueName());

                    foreach (var attribute in method.GetCustomAttributes<RunJob>().Distinct())
                    {
                        scheduler.JobFactory = new JobPublisherFactory(this.ServiceProvider);
                        var trigger = TriggerBuilder.Create().StartNow().WithCronSchedule(attribute.CronSchedule).Build();
                        var job = JobBuilder.Create(typeof(JobPublisher)).Build();
                        job.JobDataMap.Put("queue", queueName);
                        job.JobDataMap.Put("cron", attribute.CronSchedule);
                        await scheduler.ScheduleJob(job, trigger, cancellationToken);
                    }
                }
            }
        }
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }
}