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
using MassTransitRMQExtensions.Helpers;
using MassTransitRMQExtensions.Attributes.JobAttributes;
using MassTransitRMQExtensions.Attributes.ConsumerAttributes;
using System.Threading.Tasks;
using MassTransitRMQExtensions.Attributes.PublishAttributes;
using MassTransitRMQExtensions.Enums;

namespace MassTransitRMQExtensions
{
    public static class MassTransitMQConsumersConfigurator
    {
        internal static void ConfigureGenericEventConsumer<T>(IRabbitMqReceiveEndpointConfigurator receiveEndpointConfigurator, IBusRegistrationContext context, RabbitEndpoint endpoint) where T : class
        {
            receiveEndpointConfigurator.Consumer(() => new GenericEventConsumer<T>(context, endpoint));
        }
        internal static void InvokeLocalVoidGenericMethodByType(Type genericType, string methodName, object[] parameters)
        {
            var methodType = typeof(MassTransitMQConsumersConfigurator)
             .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
            var constructedMethod = methodType!.MakeGenericMethod(genericType);
            constructedMethod.Invoke(null, parameters);
        }
        internal static void ConfigureEventConsumer(this IRabbitMqReceiveEndpointConfigurator receiveEndpointConfigurator, IBusRegistrationContext context, RabbitEndpoint endpoint)
        {
            var consumerMessageType = endpoint.ConsumerMessageType;

            if (typeof(IEnumerable).IsAssignableFrom(endpoint.ConsumerMessageType) && !consumerMessageType.IsArray)
            {
                consumerMessageType = consumerMessageType.GenericTypeArguments.Single().MakeArrayType();
            }

            InvokeLocalVoidGenericMethodByType(consumerMessageType, nameof(MassTransitMQConsumersConfigurator.ConfigureGenericEventConsumer), new object[] { receiveEndpointConfigurator, context, endpoint });
        }
        internal static void ConfigureRabbitReceiveEndpoint(this IRabbitMqBusFactoryConfigurator configurator, IBusRegistrationContext context, RabbitEndpoint endpoint)
        {
            configurator.ReceiveEndpoint(endpoint.QueueName, configurator =>
            {
                configurator.ConfigureConsumeTopology = false;
                configurator.ConcurrentMessageLimit = endpoint.ConcurrentMessageLimit;
                configurator.PrefetchCount = endpoint.PrefetchCount;
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
        internal static void ConfigureMassTransitScheduledJobsEmitters(this IServiceCollection services, IEnumerable<Type> controllers, Func<string, string> queueNamingChanger)
        {
            services.AddHostedService(services =>
            {
                return new JobsHostedService(services, controllers, queueNamingChanger);
            });
        }
        internal static void RegisterPublisherMessage<T>(IRabbitMqBusFactoryConfigurator configurator, string exchange) where T : class
        {
            configurator.Message<T>(c => c.SetEntityName(exchange));
        }
        internal static void RegisterPublisherPublish<T>(IRabbitMqBusFactoryConfigurator configurator, ExchangeType exchangeType, bool resolveTopology = true) where T : class
        {
            configurator.Publish<T>(c =>
                {
                    c.ExchangeType = exchangeType.ToString().ToLower();
                    c.Exclude = !resolveTopology;
                });
        }
        internal static void RegisterPublisher(this IRabbitMqBusFactoryConfigurator configurator, RabbitMessagePublisher publisher)
        {
            InvokeLocalVoidGenericMethodByType(publisher.MessageType, nameof(RegisterPublisherMessage), new object[] { configurator, publisher.MessageExchange });
            InvokeLocalVoidGenericMethodByType(publisher.MessageType, nameof(RegisterPublisherPublish), new object[] { configurator, publisher.MessageExchangeType, publisher.ResolveTopology });
        }
        public static void ConfigureMassTransit(this IServiceCollection services, RabbitMqConfig config,
            IEnumerable<RabbitEndpoint> endpoints, IEnumerable<RabbitMessagePublisher> messagePublishers)
        {
            if (endpoints is not null && endpoints.Any())
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

                        foreach (var publish in messagePublishers)
                        {
                            cfg.RegisterPublisher(publish);
                        }

                        foreach (var endpoint in endpoints)
                        {
                            cfg.ConfigureRabbitReceiveEndpoint(context, endpoint);
                        }
                    });

                });

                services.AddMassTransitHostedService();
            }
        }
        internal static void RegisterTypesInDI(this IServiceCollection services, IEnumerable<Type> unregisteredTypes)
        {
            foreach (var type in unregisteredTypes)
            {
                if (!services.Any(x => x.ServiceType == type))
                {
                    services.AddTransient(type);
                }
            }
        }
        internal static bool ValidateControllerTypeName(this Type controllerType, Func<string, bool> controllerNameFilter = null)
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
        internal static IEnumerable<RabbitEndpoint> GetEventConsumersConfigurations(IEnumerable<Type> controllers, Func<string, string> queueNamingChanger)
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
                        EventHandler = new ControllerHandlerInfo(method.DeclaringType, method),
                        ConcurrentMessageLimit = attribute.ConcurrentMessageLimit,
                        PrefetchCount = 16
                    };
                }
            }
        }
        internal static IEnumerable<RabbitEndpoint> GetJobConfigurations(IEnumerable<Type> controllers, Func<string, string> queueNamingChanger)
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
                    ExchangeType = ExchangeType.Fanout,
                    ConsumerMessageType = typeof(JobMessage),
                    EventHandler = new ControllerHandlerInfo(method.DeclaringType, method),
                    ConcurrentMessageLimit = 1,
                    PrefetchCount = 1
                };
            }
        }
        internal static IEnumerable<RabbitMessagePublisher> GetRabbitPublishers(IEnumerable<Type> publishers)
        {
            foreach (var publisher in publishers)
            {
                var attribute = publisher.GetCustomAttributes<PublishMessage>().Single();
                yield return new RabbitMessagePublisher()
                {
                    MessageType = publisher,
                    MessageExchangeType = attribute.ExchangeType,
                    ResolveTopology = attribute.ResolveTopology,
                    MessageExchange = attribute.ExchangeName
                };
            }
        }
        public static void ConfigureMassTransitControllers(this IServiceCollection services, RabbitMqConfig config, IEnumerable<Type> controllers,
            IEnumerable<Type> publisherMessageTypes, bool configureScheduledJobEmitters = true, Func<string, string> queueNamingChanger = null)
        {
            if (queueNamingChanger is null)
            {
                queueNamingChanger = (n) => n;
            }
            var endpoints = new List<RabbitEndpoint>();

            var consumerConfigurations = GetEventConsumersConfigurations(controllers, queueNamingChanger);
            services.RegisterTypesInDI(consumerConfigurations.Select(c => c.EventHandler).Select(m => m.ControllerType).Distinct());
            endpoints.AddRange(consumerConfigurations);

            var jobConfigurations = GetJobConfigurations(controllers, queueNamingChanger);
            services.RegisterTypesInDI(jobConfigurations.Select(c => c.EventHandler).Select(m => m.ControllerType).Distinct());
            endpoints.AddRange(jobConfigurations);

            var rabbitPublishers = GetRabbitPublishers(publisherMessageTypes);

            services.ConfigureMassTransit(config, endpoints, rabbitPublishers);

            if (configureScheduledJobEmitters)
            {
                services.ConfigureMassTransitScheduledJobsEmitters(controllers, queueNamingChanger);
            }
        }
        public static void ConfigureMassTransitControllers(this IServiceCollection services, RabbitMqConfig config, IEnumerable<Assembly> assemblies,
            bool configureScheduledJobEmitters = true, Func<string, bool> controllerNameFilter = null, Func<string, string> queueNamingChanger = null)
        {
            var controllers = assemblies.SelectMany(a => a.GetExportedTypes().Where(t => t.ValidateControllerTypeName(controllerNameFilter)));
            var publisherTypes = assemblies.SelectMany(a => a.GetExportedTypes()).Where(t => t.GetCustomAttributes(typeof(PublishMessage)).Any());
            services.ConfigureMassTransitControllers(config, controllers, publisherTypes, configureScheduledJobEmitters, queueNamingChanger);
        }
        public static void ConfigureMassTransitControllers(this IServiceCollection services, RabbitMqConfig config, bool configureJobEmitters = true,
            Func<string, bool> controllerNameFilter = null, Func<string, string> queueNamingChanger = null)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(asm => !asm.IsDynamic).ToArray();
            services.ConfigureMassTransitControllers(config, assemblies, configureJobEmitters, controllerNameFilter, queueNamingChanger);
        }
        public static async Task PublishMessage<T>(this IPublishEndpoint publishEndpoint, T message, string routingKey = null) where T : class
        {
            var messageType = typeof(T);
            var messageAttributes = messageType.GetCustomAttributes().Select(a => a.GetType());
            if (!messageAttributes.Contains(typeof(PublishMessage)))
            {
                throw new Exception($"Publising message of type {messageType.Name} doesn't have routing attribute.");
            }
            await publishEndpoint.Publish(message, context => context.SetRoutingKey(routingKey) );
        }
    }
}