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

        // This method gets called by the runtime. Use this method to add services to the container.
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

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
