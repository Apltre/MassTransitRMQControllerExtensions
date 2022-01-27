using MassTransit;
using System.Reflection;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace MassTransitRMQExtensions.Models
{
    public class GenericEventConsumer<T>: IConsumer<T> where T : class
    {
        protected IServiceProvider ServiceProvider { get; }
        protected RabbitEndpoint EndpointInfo { get; }
        protected ControllerHandlerInfo HandlerInfo { get; }

        public GenericEventConsumer(IServiceProvider serviceProvider, RabbitEndpoint endpointInfo)
        {
            this.ServiceProvider = serviceProvider;
            this.EndpointInfo = endpointInfo;
            this.HandlerInfo = endpointInfo.EventHandler;
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

        protected static object InvokeHandler<M>(ControllerHandlerInfo handlerInfo, object controller, M @event)
        {
            if (typeof(M) == typeof(JobMessage))
            {
                return handlerInfo!.Method!.Invoke(controller, null);
            }
            return handlerInfo!.Method!.Invoke(controller, new object[] { @event });
        }

        protected EventLogRecord InitializeLogRecord(object @event)
        {
            return new EventLogRecord(
                this.EndpointInfo.ExchangeName,
                this.EndpointInfo.TopicRoutingKey,
                this.EndpointInfo.QueueName,
                this.EndpointInfo.ExchangeType,
                @event
                );
        }

        public async Task Consume(ConsumeContext<T> context)
        {
            using (var scope = this.ServiceProvider.CreateScope())
            {
                var serviceProvider = scope.ServiceProvider;
                var controller = serviceProvider.GetRequiredService(this.HandlerInfo.ControllerType);
                var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("RMQConsumer");
                object result = null;
                var @event = context.Message;
                var logRecord = this.InitializeLogRecord(@event);

                try
                {
                    if (IsAsync(this.HandlerInfo.Method))
                    {
                        if (!IsSimpleTask(this.HandlerInfo.Method))
                        {
                            dynamic awaitable = InvokeHandler(this.HandlerInfo, controller, @event);
                            await awaitable;
                            result = awaitable!.GetAwaiter().GetResult();
                        }
                        else
                        {
                            await (Task)InvokeHandler(this.HandlerInfo, controller, @event);
                        }
                    }
                    else
                    {
                        if (!IsVoid(this.HandlerInfo.Method))
                        {
                            result = InvokeHandler(this.HandlerInfo, controller, @event);
                        }
                        else
                        {
                            InvokeHandler(this.HandlerInfo, controller, @event);
                        }
                    }
                    logRecord.SetRecordEndState(DateTime.Now, isSuccessful: true, result: result);
                    logger.LogInformation(logRecord.ToString());
                }
                catch (Exception ex)
                {
                    logRecord.SetRecordEndState(DateTime.Now, exception: ex);
                    logger.LogInformation(logRecord.ToString());
                    throw;
                }
            }
        }
    }
}
