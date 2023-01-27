using System;
using MassTransitRMQExtensions.Abstractions;
using MassTransitRMQExtensions.Enums;

namespace MassTransitRMQExtensions.Models
{
    public class RabbitEndpoint
    {
        public RabbitEndpoint(string? exchangeName, string queueName, string? topicRoutingKey, Type consumerMessageType,
            ExchangeType exchangeType, ControllerHandlerInfo messageHandler, int concurrentMessageLimit,
            int prefetchCount, IMTRMQERetryPolicy policy, bool messageLoggingEnabled, bool messageContextUsed)
        {
            ExchangeName = exchangeName;
            QueueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
            TopicRoutingKey = topicRoutingKey;
            ConsumerMessageType = consumerMessageType ?? throw new ArgumentNullException(nameof(consumerMessageType));
            MessageContextUsed = messageContextUsed;
            ExchangeType = exchangeType;
            MessageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
            ConcurrentMessageLimit = concurrentMessageLimit;
            PrefetchCount = prefetchCount;
            Policy = policy ?? throw new ArgumentNullException(nameof(policy));
            MessageLoggingEnabled = messageLoggingEnabled;
        }

        public string? ExchangeName { get; set; }
        public string QueueName { get; set; }
        public string? TopicRoutingKey { get; set; }
        public Type ConsumerMessageType { get; set; }
        public ExchangeType ExchangeType { get; set; }
        public ControllerHandlerInfo MessageHandler { get; set;}
        public int ConcurrentMessageLimit { get; set; }
        public int PrefetchCount { get; set; }
        public IMTRMQERetryPolicy Policy { get; set; }
        public bool MessageLoggingEnabled { get; set; }
        public bool MessageContextUsed { get; set; }
    }
}
