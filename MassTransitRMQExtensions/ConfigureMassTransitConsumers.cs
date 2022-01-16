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
        private static IEnumerable<SubscribeOn> getMethodSubscribeOnAttribute(this MethodInfo method)
        {
            return method.GetCustomAttributes<SubscribeOn>();
        }
        private static bool checkMethodHasSubscribeOnAttribute(this MethodInfo method)
        {
            return method.getMethodSubscribeOnAttribute().Any();
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
        public static void ConfigureMassTransitControllers(this IServiceCollection services, RabbitMqConfig config, IEnumerable<Type> controllers, Func<string, string> queueNamingChanger = null)
        {
            if (queueNamingChanger is null)
            {
                queueNamingChanger = (n) => n;
            }
            var methodsToBind = controllers.SelectMany(t => t.GetMethods()).Where(m => m.checkMethodHasSubscribeOnAttribute()).ToList();
            services.registerTypesInDI(methodsToBind.Select(m => m.DeclaringType).Distinct());

            var endpoints = new List<RabbitEndpoint>();
            foreach (var method in methodsToBind)
            {
                var attributes = method.getMethodSubscribeOnAttribute().Distinct();

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
                    var queueName = attributes.Count() == 1 ? method.Name : $"{method.Name}_Route{attribute.Route.Replace("#", "All")}";
                    endpoints.Add(new RabbitEndpoint()
                    {
                        ExchangeName = attribute.Exchange,
                        QueueName = queueNamingChanger(queueName),
                        TopicRoutingKey = attribute.Route,
                        ExchangeType = attribute.TopologyType,
                        ConsumerMessageType = method.GetParameters().Single().ParameterType,
                        EventHandler = new ControllerHandlerInfo(method.DeclaringType, method)
                    });
                }
            }

            services.ConfigureConsumers(config, endpoints);
        }
        public static void ConfigureMassTransitControllers(this IServiceCollection services, RabbitMqConfig config, IEnumerable<Assembly> assemblies, Func<string, bool> controllerNameFilter = null, Func<string, string> queueNamingChanger = null)
        {
            var controllers = assemblies.SelectMany(a => a.GetExportedTypes().Where(t => t.validateControllerTypeName(controllerNameFilter)));
            services.ConfigureMassTransitControllers(config, controllers, queueNamingChanger);
        }
        public static void ConfigureMassTransitControllers(this IServiceCollection services, RabbitMqConfig config, Func<string, bool> controllerNameFilter = null, Func<string, string> queueNamingChanger = null)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            services.ConfigureMassTransitControllers(config, assemblies, controllerNameFilter, queueNamingChanger);
        }

    }
}