using MassTransit;
using System.Reflection;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using MassTransitRMQExtensions.Enums;
using MassTransitRMQExtensions.Abstractions;
using MassTransitRMQExtensions.Exceptions;

namespace MassTransitRMQExtensions.Models
{
    public class GenericMessageConsumer<T> : IConsumer<T> where T : class
    {
        protected IServiceProvider ServiceProvider { get; }
        protected RabbitEndpoint EndpointInfo { get; }
        protected ControllerHandlerInfo HandlerInfo { get; }

        public GenericMessageConsumer(IServiceProvider serviceProvider, RabbitEndpoint endpointInfo)
        {
            ServiceProvider = serviceProvider;
            EndpointInfo = endpointInfo;
            HandlerInfo = endpointInfo.MessageHandler;
        }
        protected static bool IsAsync(MethodInfo method)
        {
            return !(method.ReturnType.GetMethod("GetAwaiter") is null);
        }
        protected static bool IsVoid(MethodInfo method)
        {
            return method.ReturnType == typeof(void);
        }
        protected static bool IsSimpleTask(MethodInfo method)
        {
            return method.ReturnType == typeof(Task);
        }
        protected static object InvokeHandler<M>(ControllerHandlerInfo handlerInfo, object controller, M message) where M : notnull 
        {
            if (message is JobMessage)
            {
                return handlerInfo!.Method!.Invoke(controller, null);
            }
            return handlerInfo!.Method!.Invoke(controller, new object[] { message });
        }
        protected MessageLogRecord InitializeLogRecord(object? message, int? retryAttempt = null)
        {
            return new MessageLogRecord(
                EndpointInfo.ExchangeName,
                EndpointInfo.TopicRoutingKey,
                EndpointInfo.QueueName,
                EndpointInfo.ExchangeType,
                message,
                retryAttempt
                );
        }
        protected static object CastToGenericList<L>(IEnumerable ie)
        {
            var list = new List<L>();
            foreach (var item in ie)
            {
                list.Add((L)item);
            }
            return list;
        }
        protected static object CastToListByType(Type innerType, IEnumerable ie)
        {
            var methodType = typeof(GenericMessageConsumer<T>)
                .GetMethod(nameof(GenericMessageConsumer<T>.CastToGenericList), BindingFlags.NonPublic | BindingFlags.Static);
            var constructedMethod = methodType!.MakeGenericMethod(innerType);
            return constructedMethod.Invoke(null, new object[] { ie });
        }
        protected static object EqualizeMessageType(T input, Type outputType)
        {
            if (outputType.IsGenericType)
            {
                switch (outputType.GetGenericTypeDefinition())
                {
                    case Type listType when listType == typeof(List<>):
                        var innerType = outputType.GetGenericArguments().Single();
                        return CastToListByType(innerType, (IEnumerable)input);
                    default:
                        return input;
                }
            }
            return input;
        }
        internal async Task SendMessageOnRetry(ISendEndpointProvider sendEndpointProvider, T message, int retryAttempt, TimeSpan delay, bool useDlxSuffix = false)
        {
            var dlxSuffix = useDlxSuffix ? $"_{(int)delay.TotalSeconds}" : null;
            var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"exchange:{EndpointInfo.QueueName}{dlxSuffix}_DLX"));
            await endpoint.Send(message, context =>
            {
                context.Headers.Set("MT-Redelivery-Count", retryAttempt);
                context.TimeToLive = delay;
            });
        }
        internal async Task<bool> CheckAndUseRMQRetry(IServiceProvider serviceProvider, T message, IMTRMQERetryPolicy retryPolicy, int retryAttempt)
        {
            var sendEndpointProvider = serviceProvider.GetRequiredService<ISendEndpointProvider>();
            switch (retryPolicy)
            {
                case RetryPolicyImmediate r:
                    if (retryAttempt == r.RetryLimit)
                    {
                        return false;
                    }
                    await SendMessageOnRetry(sendEndpointProvider, message, retryAttempt + 1, TimeSpan.Zero);
                    break;
                case RetryPolicyInterval r:
                    if (retryAttempt == r.RetryLimit)
                    {
                        return false;
                    }
                    await SendMessageOnRetry(sendEndpointProvider, message, retryAttempt + 1, r.Interval);
                    break;
                case RetryPolicyExponential r:
                    if (retryAttempt == r.RetryLimit)
                    {
                        return false;
                    }
                    var retryDelaysCount = r.RetryDelaysInSeconds.Count;
                    var intervalSeconds = retryAttempt < retryDelaysCount ? r.RetryDelaysInSeconds[retryAttempt] : r.RetryDelaysInSeconds[retryDelaysCount - 1];
                    await SendMessageOnRetry(sendEndpointProvider, message, retryAttempt + 1, TimeSpan.FromSeconds(intervalSeconds), true);
                    break;
                default:
                    throw new Exception($"Unsupported retry type for RMQ retry policy");
            }
            return true;
        }
        internal static object GetContextWithGenericMessage<M>(RabbitEndpoint endpoint, object? message, Headers headers) where M: class
        {
            return new MsgContext<M>(
                message: (M)message,
                exchange: endpoint.ExchangeName,
                exchangeType: endpoint.ExchangeType,
                route: endpoint.TopicRoutingKey,
                headers: headers);
        }
        internal object GetContextWithMessage(RabbitEndpoint endpoint, object? message, Headers headers)
        {
            var methodType = typeof(GenericMessageConsumer<T>)
            .GetMethod(nameof(GetContextWithGenericMessage), BindingFlags.NonPublic | BindingFlags.Static);
            var constructedMethod = methodType!.MakeGenericMethod(endpoint.ConsumerMessageType);
            return constructedMethod.Invoke(null, new object[] { endpoint, message, headers });
        }
        public async Task Consume(ConsumeContext<T> context)
        {
            var retryAttempt = EndpointInfo.Policy.RetryPolicyType == RetryPolicyType.RabbitMq ? (context.Headers.Get("MT-Redelivery-Count", default(int?)) ?? 0) : (int?)context.GetRetryAttempt();

            var recordMessage = EndpointInfo.MessageLoggingEnabled ? context.Message : null;
            var logRecord = InitializeLogRecord(recordMessage, retryAttempt);
            using var scope = ServiceProvider.CreateScope();
            var serviceProvider = scope.ServiceProvider;
            var logger = serviceProvider.GetRequiredService<ILogger<GenericMessageConsumer<T>>>();

            try
            {
                var controller = serviceProvider.GetRequiredService(HandlerInfo.ControllerType);

                object? result = null;
                var message = EqualizeMessageType(context.Message, EndpointInfo.ConsumerMessageType);

                if (EndpointInfo.MessageContextUsed)
                {
                    message = GetContextWithMessage(EndpointInfo, message, context.Headers);
                }

                if (IsAsync(HandlerInfo.Method))
                {
                    if (!IsSimpleTask(HandlerInfo.Method))
                    {
                        dynamic awaitable = InvokeHandler(HandlerInfo, controller, message);
                        await awaitable;
                        result = awaitable!.GetAwaiter().GetResult();
                    }
                    else
                    {
                        await (Task)InvokeHandler(HandlerInfo, controller, message);
                    }
                }
                else
                {
                    if (!IsVoid(HandlerInfo.Method))
                    {
                        result = InvokeHandler(HandlerInfo, controller, message);
                    }
                    else
                    {
                        InvokeHandler(HandlerInfo, controller, message);
                    }
                }
                logRecord.SetRecordEndState(DateTime.Now, isSuccessful: true, result: result);
                logger.LogInformation(logRecord.ToString());
            }
            catch (Exception ex)
            {
                logRecord.SetRecordEndState(DateTime.Now, exception: ex);
                logger.LogInformation(logRecord.ToString());

                var retryPolicy = EndpointInfo.Policy;
                var failFastException = (ex as MTConsumerFailFastException)
                    ?? (ex?.InnerException as MTConsumerFailFastException);

                if (!(failFastException is null))
                {
                    switch(EndpointInfo.Policy.RetryPolicyType)
                    {
                        case RetryPolicyType.RabbitMq: 
                            throw;
                        default:
                            throw failFastException;
                    }
                }

                if (EndpointInfo.Policy.RetryPolicyType != RetryPolicyType.RabbitMq)
                {
                    throw;
                }

                if (!await CheckAndUseRMQRetry(serviceProvider, context.Message, retryPolicy, retryAttempt!.Value))
                {
                    throw;
                }
            }
        }
    }
}