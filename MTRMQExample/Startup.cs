using MassTransitRMQExtensions;
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
            var rabbitConfig = this.Configuration.GetSection("Rabbit");
            var conf = new RabbitMqConfig()
            {
                UserName = rabbitConfig["UserName"],
                Password = rabbitConfig["Password"],
                Host = new Uri($"amqp://{rabbitConfig["HostName"]}:{rabbitConfig["Port"]}")
            };

            services.ConfigureMassTransitControllers(conf, controllerNameFilter: (controllerName) => controllerName.Contains("Test"), queueNamingChanger: (queueName) => $"{queueName}_v1");

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
