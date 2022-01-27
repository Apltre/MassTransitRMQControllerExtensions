using MassTransit;
using MassTransit.RabbitMqTransport;
using System.Reflection;
using GreenPipes;
using System.Collections;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using MassTransitRMQExtensions.Models;
using MassTransitRMQExtensions.Attributes;
using MassTransitRMQExtensions.Helpers;

namespace MassTransitRMQExtensions
{
    public static class MassTransitMQConsumersConfigurator
    {
        private static void ConfigureGenericEventConsumer<T>(IRabbitMqReceiveEndpointConfigurator receiveEndpointConfigurator, IBusRegistrationContext context, RabbitEndpoint endpoint) where T : class
        {
            receiveEndpointConfigurator.Consumer(() => new GenericEventConsumer<T>(context, endpoint));
        }
        private static void ConfigureEventConsumer(this IRabbitMqReceiveEndpointConfigurator receiveEndpointConfigurator, IBusRegistrationContext context, RabbitEndpoint endpoint)
        {
            var methodType = typeof(MassTransitMQConsumersConfigurator)
                .GetMethod(nameof(MassTransitMQConsumersConfigurator.ConfigureGenericEventConsumer), BindingFlags.NonPublic | BindingFlags.Static);

            var consumerMessageType = endpoint.ConsumerMessageType;

            if (typeof(IEnumerable).IsAssignableFrom(endpoint.ConsumerMessageType) && !consumerMessageType.IsArray)
            {
                consumerMessageType = consumerMessageType.GenericTypeArguments.Single().MakeArrayType();
            }

            var constructedMethod = methodType!.MakeGenericMethod(consumerMessageType);
            constructedMethod.Invoke(null, new object[] { receiveEndpointConfigurator, context, endpoint });
        }
      
        private static void ConfigureRabbitReceiveEndpoint(this IRabbitMqBusFactoryConfigurator configurator, IBusRegistrationContext context, RabbitEndpoint endpoint)
        {
            configurator.ReceiveEndpoint(endpoint.QueueName, configurator =>
            {
                configurator.ConfigureConsumeTopology = false;
                configurator.Bind(endpoint.ExchangeName,
                    bc =>
                    {
                        bc.RoutingKey = endpoint.TopicRoutingKey;
                        bc.Durable = true;
                        bc.ExchangeType = endpoint.ExchangeType.ToString().ToLower();
                    });

                configurator.ConfigureEventConsumer(context, endpoint);
            });
        }
        private static void configureMassTransitScheduledJobsEmitters(this IServiceCollection services, IEnumerable<Type> controllers, Func<string, string> queueNamingChanger)
        {
            services.AddHostedService(services =>
            {
                return new JobsHostedService(services, controllers, queueNamingChanger);
            });
        }
        public static void ConfigureConsumers(this IServiceCollection services, RabbitMqConfig config, IEnumerable<RabbitEndpoint> endpoints)
        {
            if (!(endpoints is null) && endpoints.Any())
            {
                services.AddMassTransit(bus =>
                {
                    bus.UsingRabbitMq((context, cfg) =>
                    {
                        cfg.ClearMessageDeserializers();
                        cfg.UseRawJsonSerializer();

                        cfg.Host(config.Host,
                                 hc =>
                                 {
                                     hc.Username(config.UserName);
                                     hc.Password(config.Password);
                                 });

                        cfg.UseMessageRetry(r => r.Exponential(21,
                                                                  TimeSpan.FromMinutes(2),
                                                                  TimeSpan.FromMinutes(20),
                                                                  TimeSpan.FromMinutes(1))); //~ 6 hours
                        foreach (var endpoint in endpoints)
                        {
                            cfg.ConfigureRabbitReceiveEndpoint(context, endpoint);
                        }
                    });

                });

                services.AddMassTransitHostedService();
            }
        }
        private static void registerTypesInDI(this IServiceCollection services, IEnumerable<Type> unregisteredTypes)
        {
            foreach (var type in unregisteredTypes)
            {
                if (!services.Any(x => x.ServiceType == type))
                {
                    services.AddTransient(type);
                }
            }
        }
  
        private static bool validateControllerTypeName(this Type controllerType, Func<string, bool> controllerNameFilter = null)
        {
            var controllerName = controllerType.Name;

            if (controllerNameFilter is null)
            {
                controllerNameFilter = (n) => true;
            }

            return controllerName.EndsWith("Controller")
                && controllerNameFilter(controllerName)
                && !controllerType.IsGenericType;
        }
        private static IEnumerable<RabbitEndpoint> getEventConsumersConfigurations(IEnumerable<Type> controllers, Func<string, string> queueNamingChanger)
        {
            var methodsToBind = controllers.SelectMany(t => t.GetMethods()).Where(m => m.CheckMethodHasAttribute<SubscribeOn>()).ToList();
            foreach (var method in methodsToBind)
            {
                var attributes = method.GetCustomAttributes<SubscribeOn>().Distinct();

                if (attributes.Select(a => a.Exchange).Distinct().Count() > 1)
                {
                    throw new Exception($"Method {method.Name} has multiple exchange declarations.");
                }

                if (attributes.Select(a => a.TopologyType).Distinct().Count() > 1)
                {
                    throw new Exception($"Method {method.Name} has multiple exchange type declarations for exchange {attributes.First().Exchange}.");
                }

                foreach (var attribute in attributes)
                {
                    var name = method.GetQueueName();
                    var queueName = attributes.Count() == 1 ? name : $"{name}_Route{attribute.Route.Replace("#", "All")}";
                    yield return new RabbitEndpoint()
                    {
                        ExchangeName = attribute.Exchange,
                        QueueName = queueNamingChanger(queueName),
                        TopicRoutingKey = attribute.Route,
                        ExchangeType = attribute.TopologyType,
                        ConsumerMessageType = method.GetParameters().Single().ParameterType,
                        EventHandler = new ControllerHandlerInfo(method.DeclaringType, method)
                    };
                }
            }
        }
        private static IEnumerable<RabbitEndpoint> getJobConfigurations(IEnumerable<Type> controllers, Func<string, string> queueNamingChanger)
        {
            var methodsToBind = controllers.SelectMany(t => t.GetMethods()).Where(m => m.CheckMethodHasAttribute<RunJob>()).ToList();
            foreach (var method in methodsToBind)
            {
                var name = queueNamingChanger(method.GetQueueName());
                yield return new RabbitEndpoint()
                {
                    ExchangeName = name,
                    QueueName = name,
                    TopicRoutingKey = "",
                    ExchangeType = Enums.ExchangeType.Fanout,
                    ConsumerMessageType = typeof(JobMessage),
                    EventHandler = new ControllerHandlerInfo(method.DeclaringType, method)
                };
            }
        }
        public static void ConfigureMassTransitControllers(this IServiceCollection services, RabbitMqConfig config, IEnumerable<Type> controllers,
            bool configureScheduledJobEmitters = true, Func<string, string> queueNamingChanger = null)
        {
            if (queueNamingChanger is null)
            {
                queueNamingChanger = (n) => n;
            }
            var endpoints = new List<RabbitEndpoint>();

            var consumerConfigurations = getEventConsumersConfigurations(controllers, queueNamingChanger);
            services.registerTypesInDI(consumerConfigurations.Select(c => c.EventHandler).Select(m => m.ControllerType).Distinct());
            endpoints.AddRange(consumerConfigurations);

            var jobConfigurations = getJobConfigurations(controllers, queueNamingChanger);
            services.registerTypesInDI(jobConfigurations.Select(c => c.EventHandler).Select(m => m.ControllerType).Distinct());
            endpoints.AddRange(jobConfigurations);

            services.ConfigureConsumers(config, endpoints);

            if (configureScheduledJobEmitters)
            {
                services.configureMassTransitScheduledJobsEmitters(controllers, queueNamingChanger);
            }
        }
        public static void ConfigureMassTransitControllers(this IServiceCollection services, RabbitMqConfig config, IEnumerable<Assembly> assemblies,
            bool configureScheduledJobEmitters = true, Func<string, bool> controllerNameFilter = null, Func<string, string> queueNamingChanger = null)
        {
            var controllers = assemblies.SelectMany(a => a.GetExportedTypes().Where(t => t.validateControllerTypeName(controllerNameFilter)));
            services.ConfigureMassTransitControllers(config, controllers, configureScheduledJobEmitters, queueNamingChanger);
        }
        public static void ConfigureMassTransitControllers(this IServiceCollection services, RabbitMqConfig config, bool configureJobSchedulers = true,
            Func<string, bool> controllerNameFilter = null, Func<string, string> queueNamingChanger = null)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            services.ConfigureMassTransitControllers(config, assemblies, configureJobSchedulers, controllerNameFilter, queueNamingChanger);
        }
    }
}