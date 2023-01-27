using MassTransit;
using MassTransitRMQExtensions.Abstractions;
using MassTransitRMQExtensions.Attributes.ConsumerAttributes;
using MassTransitRMQExtensions.Attributes.JobAttributes;
using MassTransitRMQExtensions.Attributes.PublishAttributes;
using MassTransitRMQExtensions.Enums;
using MassTransitRMQExtensions.Helpers;
using MassTransitRMQExtensions.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MassTransitRMQExtensions.Converters.Dynamic;
using MassTransitRMQExtensions.Exceptions;

namespace MassTransitRMQExtensions
{
    public static class MassTransitMQConsumersConfigurator
    {
        internal static readonly IMTRMQERetryPolicy DefaultJobRetryPolicy = new RetryPolicyExponential(RetryPolicyType.Masstransit,
            21, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(1));
        internal static readonly IMTRMQERetryPolicy DefaultConsumerRetryPolicy = new RetryPolicyInterval(RetryPolicyType.RabbitMq, 72, TimeSpan.FromMinutes(5));
        internal static readonly HashSet<Type> registeredPublishTypes = new HashSet<Type>();
        internal static void ConfigureGenericMessageConsumer<T>(IRabbitMqReceiveEndpointConfigurator receiveEndpointConfigurator, IBusRegistrationContext context, RabbitEndpoint endpoint) where T : class
        {
            receiveEndpointConfigurator.Consumer(() => new GenericMessageConsumer<T>(context, endpoint));
        }
        internal static void InvokeLocalVoidGenericMethodByType(Type genericType, string methodName, object[] parameters)
        {
            var methodType = typeof(MassTransitMQConsumersConfigurator)
             .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
            var constructedMethod = methodType!.MakeGenericMethod(genericType);
            constructedMethod.Invoke(null, parameters);
        }
        internal static void ConfigureMessageConsumer(this IRabbitMqReceiveEndpointConfigurator receiveEndpointConfigurator, IBusRegistrationContext context, RabbitEndpoint endpoint)
        {
            var consumerMessageType = endpoint.ConsumerMessageType;
            var isIEnumerable = typeof(IEnumerable).IsAssignableFrom(endpoint.ConsumerMessageType);

            if (!(consumerMessageType.IsGenericType || consumerMessageType.IsArray) && isIEnumerable)
            {
                throw new Exception("For an array batched messages use types IEnumerable<T>, T[], List<T>!");
            }

            if (isIEnumerable && !consumerMessageType.IsArray)
            {
                consumerMessageType = consumerMessageType.GenericTypeArguments.Single().MakeArrayType();
            }

            InvokeLocalVoidGenericMethodByType(consumerMessageType, nameof(MassTransitMQConsumersConfigurator.ConfigureGenericMessageConsumer), new object[] { receiveEndpointConfigurator, context, endpoint });
        }
        internal static void ConfigureMTRetry(this IRabbitMqReceiveEndpointConfigurator configurator, IMTRMQERetryPolicy retryPolicy)
        {
            Action<IRetryConfigurator> action = retryPolicy switch
            {
                RetryPolicyImmediate r => (conf) => conf.Immediate(r.RetryLimit),
                RetryPolicyIncremental r => (conf) => conf.Incremental(r.RetryLimit, r.InitialInterval, r.IntervalIncrement),
                RetryPolicyInterval r => (conf) => conf.Interval(r.RetryLimit, r.Interval),
                RetryPolicyIntervals r => (conf) => conf.Intervals(r.Intervals.ToArray()),
                RetryPolicyExponential r => (conf) => conf.Exponential(r.RetryLimit, r.MinInterval, r.MaxInterval, r.IntervalDelta),
                _ => throw new Exception("Unknown MT retry configurator."),
            };
            configurator.UseMessageRetry(r => {
                action(r);
                r.Ignore<MTConsumerFailFastException>();
            });
        }
        internal static void BindDLXQueue(this IRabbitMqReceiveEndpointConfigurator configurator, string queueName, string? suffix = null)
        {
            suffix = suffix != null ? $"_{suffix}" : null;
            configurator.BindDeadLetterQueue($"{queueName}{suffix}_DLX", $"{queueName}{suffix}_DLX",
                        bc =>
                        {
                            bc.SetQueueArgument("x-dead-letter-exchange", queueName);
                            bc.Durable = true;
                        });
        }
        internal static void ConfigureRMQDeadLetterExchange(this IRabbitMqReceiveEndpointConfigurator configurator, RabbitEndpoint endpoint)
        {
            switch (endpoint.Policy)
            {
                case RetryPolicyImmediate _:
                case RetryPolicyInterval _:
                    configurator.BindDLXQueue(endpoint.QueueName);
                    break;
                case RetryPolicyExponential r:              
                    var intervalsSeconds = r.RetryDelaysInSeconds;
                    foreach (var interval in intervalsSeconds)
                    {
                        configurator.BindDLXQueue(endpoint.QueueName, interval.ToString());
                    }
                    break;
                default:
                    throw new Exception($"Unsupported RMQ DLX for retry policy");
            }

            //clear argument after BindDeadLetterQueue. we dont need dlx on 'endpoint.QueueName'
            configurator.SetQueueArgument("x-dead-letter-exchange", null);
        }
        internal static void ConfigureRabbitReceiveEndpoint(this IRabbitMqBusFactoryConfigurator configurator, IBusRegistrationContext context, RabbitEndpoint endpoint)
        {
            configurator.ReceiveEndpoint(endpoint.QueueName, configurator =>
            {
                configurator.ConfigureConsumeTopology = false;
                configurator.ConcurrentMessageLimit = endpoint.ConcurrentMessageLimit;
                configurator.PrefetchCount = endpoint.PrefetchCount;

                if (!string.IsNullOrWhiteSpace(endpoint.ExchangeName))
                {
                    configurator.Bind(endpoint.ExchangeName,
                    bc =>
                    {
                        bc.RoutingKey = endpoint.TopicRoutingKey ?? "";
                        bc.Durable = true;
                        bc.ExchangeType = endpoint.ExchangeType.ToString().ToLower();
                    });
                }

                switch (endpoint.Policy.RetryPolicyType)
                {
                    case RetryPolicyType.None:
                        break;
                    case RetryPolicyType.Masstransit:
                        configurator.ConfigureMTRetry(endpoint.Policy);
                        break;
                    case RetryPolicyType.RabbitMq:
                        configurator.ConfigureRMQDeadLetterExchange(endpoint);
                        break;
                    default:
                        throw new Exception($"Unexpected RetryPolicyType {endpoint.Policy.RetryPolicyType}");
                }

                configurator.ConfigureMessageConsumer(context, endpoint);
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
            registeredPublishTypes.Add(publisher.MessageType);
        }
        public static void ConfigureMassTransit(this IServiceCollection services, RabbitMqConfig config,
            IEnumerable<RabbitEndpoint> endpoints, IEnumerable<RabbitMessagePublisher> messagePublishers)
        {
            if (endpoints is null)
            {
                endpoints = new List<RabbitEndpoint>();
            }

            if (messagePublishers is null)
            {
                messagePublishers = new List<RabbitMessagePublisher>();
            }

            services.AddMassTransit(bus =>
            {   
                bus.UsingRabbitMq((context, cfg) =>
                {
                    cfg.ClearSerialization();
                    cfg.UseRawJsonSerializer();
                    
                    cfg.ConfigureJsonSerializerOptions(options =>
                    {
                        options.Converters.Add(new DynamicConverter());
                        return options;
                    });

                    cfg.Host(config.Host,
                             hc =>
                             {
                                 hc.Username(config.UserName);
                                 hc.Password(config.Password);
                             });

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
        internal static bool ValidateControllerTypeName(this Type controllerType, Func<string, bool>? controllerNameFilter = null)
        {
            var controllerName = controllerType.Name;

            if (controllerNameFilter is null)
            {
                controllerNameFilter = (n) => true;
            }

            return controllerNameFilter(controllerName)
                && !controllerType.IsGenericType;
        }
        internal static string GetQueueNameForMethodWithMultipleAttributes(string queueName, SubscribeOn attribute)
        {
            var titleCaseExchange = new CultureInfo("en-US").TextInfo.ToTitleCase(attribute.Exchange);
            queueName = $"{queueName}_Exchange{titleCaseExchange}";
            if (attribute.TopologyType != ExchangeType.Fanout)
            {
                queueName = $"{queueName}_Route";

                var route = attribute.Route;

                switch (attribute.TopologyType)
                {
                    case ExchangeType.Direct:
                        route = route?.Replace("#", "Sharp");
                        break;
                    case ExchangeType.Topic:
                        route = route?.Replace("#", "All");
                        break;
                    default:
                        break;
                }
                queueName = $"{queueName}{route}";
            }
            return queueName;
        }
        internal static (bool messageContextUsed, Type messageType) GetConsumerMethodMessageType(MethodInfo method)
        {
            var consumerParameterType = method.GetParameters().Single().ParameterType;

            if (consumerParameterType.IsGenericType && consumerParameterType.GetGenericTypeDefinition() == typeof(MsgContext<>))
            {
                return (true, consumerParameterType.GenericTypeArguments.Single());
            }

            return (false, consumerParameterType);
        }
        internal static IEnumerable<RabbitEndpoint> GetMessageConsumersConfigurations(IEnumerable<Type> controllers, Func<string, string> queueNamingChanger,
            Func<string, string> consumerExchangeNamingChanger, IMTRMQERetryPolicy globalRetryPolicy, bool messageLoggingEnabled)
        {
            var methodsToBind = controllers.SelectMany(t => t.GetMethods()).Where(m => m.CheckMethodHasAttribute<SubscribeOn>()).ToList();
            foreach (var method in methodsToBind)
            {
                var methodAttributes = method.GetCustomAttributes<SubscribeOn>().Distinct();
                var attributeGroups = methodAttributes.GroupBy(a => a.Exchange);
                foreach (var attributeGroup in attributeGroups)
                {
                    if (attributeGroup.Select(a => a.TopologyType).Distinct().Count() > 1)
                    {
                        throw new Exception($"Method {method.Name} has multiple exchange type declarations for exchange {attributeGroup.First().Exchange}.");
                    }

                    if (attributeGroup.Select(a => (a.Exchange, a.TopologyType, a.Route)).Distinct().Count() < attributeGroup.Count())
                    {
                        throw new Exception($"Method {method.Name} has multiple attributes with the same exchange, topology, route but different retry pattern.");
                    }

                    foreach (var attribute in attributeGroup)
                    {
                        var name = method.GetQueueName();
                        var queueName = methodAttributes.Count() == 1 ? name : GetQueueNameForMethodWithMultipleAttributes(name, attribute);
                        var (messageContextUsed, messageType) = GetConsumerMethodMessageType(method);
                        yield return new RabbitEndpoint(
                            exchangeName: !string.IsNullOrWhiteSpace(attribute.Exchange) ? consumerExchangeNamingChanger(attribute.Exchange) : null,
                            queueName: queueNamingChanger(queueName),
                            topicRoutingKey: attribute.Route,
                            exchangeType: attribute.TopologyType,
                            consumerMessageType: messageType,
                            messageHandler: new ControllerHandlerInfo(method.DeclaringType, method),
                            concurrentMessageLimit: attribute.ConcurrentMessageLimit,
                            prefetchCount: 16,
                            policy: string.IsNullOrWhiteSpace(attribute.RetryPolicy) ? globalRetryPolicy : RetryPolicyParser.Parse(attribute.RetryPolicy),
                            messageLoggingEnabled: messageLoggingEnabled,
                            messageContextUsed: messageContextUsed
                        );
                    }
                }
            }
        }
        internal static IEnumerable<RabbitEndpoint> GetJobConfigurations(IEnumerable<Type> controllers, Func<string, string> queueNamingChanger,
            bool messageLoggingEnabled)
        {
            var methodsToBind = controllers.SelectMany(t => t.GetMethods()).Where(m => m.CheckMethodHasAttribute<RunJob>()).ToList();
            foreach (var method in methodsToBind)
            {
                var name = queueNamingChanger(method.GetQueueName());
                yield return new RabbitEndpoint(
                    exchangeName: null,
                    queueName : name,
                    topicRoutingKey : null,
                    exchangeType : ExchangeType.None,
                    consumerMessageType : typeof(JobMessage),
                    messageHandler : new ControllerHandlerInfo(method.DeclaringType, method),
                    concurrentMessageLimit : 1,
                    prefetchCount : 1,
                    policy : DefaultJobRetryPolicy,
                    messageLoggingEnabled : messageLoggingEnabled,
                    messageContextUsed: false
                );
            }
        }
        internal static IEnumerable<RabbitMessagePublisher> GetRabbitPublishers(IEnumerable<Type> publishers)
        {
            foreach (var publisher in publishers)
            {
                var attribute = publisher.GetCustomAttributes<PublishMessage>().Single();
                yield return new RabbitMessagePublisher(
                    messageType: publisher,
                    messageExchangeType: attribute.ExchangeType,
                    resolveTopology: attribute.ResolveTopology,
                    messageExchange: attribute.ExchangeName
                );
            }
        }
        public static void ConfigureMassTransitControllers(this IServiceCollection services, RabbitMqConfig config, IEnumerable<Type> controllers,
            IEnumerable<Type> publisherMessageTypes, bool configureScheduledJobEmitters = true, Func<string, string>? queueNamingChanger = null, 
            Func<string, string>? consumerExchangeNamingChanger = null, IMTRMQERetryPolicy? globalRetryPolicy = null, bool messageLoggingEnabled = true)
        {
            if (controllers is null)
            {
                controllers = new List<Type>();
            }

            if (publisherMessageTypes is null)
            {
                publisherMessageTypes = new List<Type>();
            }

            if (queueNamingChanger is null)
            {
                queueNamingChanger = (n) => n;
            }

            if (consumerExchangeNamingChanger is null)
            {
                consumerExchangeNamingChanger = (n) => n;
            }

            if (globalRetryPolicy is null)
            {
                globalRetryPolicy = DefaultConsumerRetryPolicy;
            }

            var endpoints = new List<RabbitEndpoint>();

            var consumerConfigurations = GetMessageConsumersConfigurations(controllers, queueNamingChanger,
                consumerExchangeNamingChanger, globalRetryPolicy, messageLoggingEnabled).ToList();
            services.RegisterTypesInDI(consumerConfigurations.Select(c => c.MessageHandler).Select(m => m.ControllerType).Distinct());
            endpoints.AddRange(consumerConfigurations);

            var jobConfigurations = GetJobConfigurations(controllers, queueNamingChanger, messageLoggingEnabled).ToList();
            services.RegisterTypesInDI(jobConfigurations.Select(c => c.MessageHandler).Select(m => m.ControllerType).Distinct());
            endpoints.AddRange(jobConfigurations);

            var rabbitPublishers = GetRabbitPublishers(publisherMessageTypes);

            services.ConfigureMassTransit(config, endpoints, rabbitPublishers);

            if (configureScheduledJobEmitters)
            {
                services.ConfigureMassTransitScheduledJobsEmitters(controllers, queueNamingChanger);
            }
        }
        public static void ConfigureMassTransitControllers(this IServiceCollection services, RabbitMqConfig config, IEnumerable<Assembly> assemblies,
            bool configureScheduledJobEmitters = true, Func<string, bool>? controllerNameFilter = null, Func<string, string>? queueNamingChanger = null,
            Func<string, string>? consumerExchangeNamingChanger = null, IMTRMQERetryPolicy? globalRetryPolicy = null, bool messageLoggingEnabled = true)
        {
            if (assemblies is null)
            {
                assemblies = new List<Assembly>();
            }

            var controllers = assemblies.SelectMany(a => a.GetExportedTypes().Where(t => t.ValidateControllerTypeName(controllerNameFilter)));
            var publisherTypes = assemblies.SelectMany(a => a.GetExportedTypes()).Where(t => t.GetCustomAttributes(typeof(PublishMessage)).Any());
            services.ConfigureMassTransitControllers(config, controllers, publisherTypes, configureScheduledJobEmitters, queueNamingChanger,
                consumerExchangeNamingChanger, globalRetryPolicy, messageLoggingEnabled);
        }
        public static void ConfigureMassTransitControllers(this IServiceCollection services, RabbitMqConfig config, bool configureJobEmitters = true,
            Func<string, bool>? controllerNameFilter = null, Func<string, string>? queueNamingChanger = null, 
            Func<string, string>? consumerExchangeNamingChanger = null, IMTRMQERetryPolicy? globalRetryPolicy = null,
            bool messageLoggingEnabled = true)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(asm => !asm.IsDynamic).ToArray();
            services.ConfigureMassTransitControllers(config, assemblies, configureJobEmitters, controllerNameFilter, queueNamingChanger,
                consumerExchangeNamingChanger, globalRetryPolicy, messageLoggingEnabled);
        }
        public static async Task PublishMessage<T>(this IPublishEndpoint publishEndpoint, T message, string? routingKey = null) where T : class
        {
            var messageType = typeof(T);
            if (!registeredPublishTypes.Contains(messageType))
            {
                throw new Exception($"Message of type {messageType} wasn't registered for publishing. Message has no routing attribute or check if message library was in the list of runtime libraries before MT registration.");
            }
            await publishEndpoint.Publish(message, context => context.SetRoutingKey(routingKey));
        }
    }
}