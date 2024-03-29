﻿using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MassTransitRMQExtensions.Models
{
    internal class JobPublisher : IJob
    {
        public JobPublisher(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            SerializerOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }
        public IServiceProvider ServiceProvider { get; }
        public JsonSerializerOptions SerializerOptions { get; }

        public async Task Execute(IJobExecutionContext context)
        {
            var queue = (string)context.JobDetail.JobDataMap["queue"];
            var queueUri = new Uri($"exchange:{queue}");
            var cron = (string)context.JobDetail.JobDataMap["cron"];

            var cronExpr = new CronExpression(cron);
            var ttl = (cronExpr.GetTimeAfter(DateTimeOffset.UtcNow)!.Value.ToLocalTime() - DateTime.Now) / 4;

            using var scope = ServiceProvider.CreateScope();
            var endpointProvider = scope.ServiceProvider.GetRequiredService<ISendEndpointProvider>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<JobPublisher>>();
            try
            {
                var endpoint = await endpointProvider.GetSendEndpoint(queueUri);
                await endpoint.Send(new JobMessage(), context => context.TimeToLive = ttl);
            }
            catch (Exception ex)
            {
                var json = JsonSerializer.Serialize(new
                {
                    Type = "Job emitter exception",
                    QueueLink = queue,
                    Exception = ex.ToString(),
                    DateTime = DateTime.Now
                }, SerializerOptions);
                logger.LogInformation(json);
            }
        }
    }
}