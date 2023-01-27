using MassTransitRMQExtensions;
using MassTransitRMQExtensions.Enums;
using MassTransitRMQExtensions.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MTRMQExample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();
            var rabbitConfig = Configuration.GetSection("Rabbit");
            var conf = new RabbitMqConfig(           
                userName : rabbitConfig["UserName"],
                password : rabbitConfig["Password"],
                host : new Uri($"amqp://{rabbitConfig["HostName"]}:{rabbitConfig["Port"]}")
            );

            //var policy = new RetryPolicyInterval(MassTransitRMQExtensions.Enums.RetryPolicyType.RabbitMq, 3, new TimeSpan(0, 0, 20));
            //patterns are doubles of minutes
            //var policy = new RetryPolicyInterval("rmq intvl 3 0.33");

            //var policy = new RetryPolicyNone();

            //var policy = new RetryPolicyExponential("mt exp 2 1 2 1");
            //var policy = new RetryPolicyExponential(RetryPolicyType.Masstransit, 2, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(1));

            //var jobPolicy = new RetryPolicyExponential(RetryPolicyType.Masstransit, 21, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(1)); retry policy for jobs
            var policy = new RetryPolicyInterval(RetryPolicyType.RabbitMq, 72, TimeSpan.FromMinutes(5)); //global default for consumers
            services.ConfigureMassTransitControllers(conf, controllerNameFilter: (controllerName) => controllerName.Contains("Test"),
                queueNamingChanger: (queueName) => $"{queueName}_v1",
                consumerExchangeNamingChanger: (ex) => $"{ex}_v1",
                globalRetryPolicy: policy);

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}