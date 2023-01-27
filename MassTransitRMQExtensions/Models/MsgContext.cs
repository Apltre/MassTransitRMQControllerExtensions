using MassTransit;
using MassTransitRMQExtensions.Enums;

namespace MassTransitRMQExtensions.Models
{
    public class MsgContext<T> where T : class
    {
        public MsgContext(T message, string? exchange, ExchangeType exchangeType, string? route, Headers headers)
        {
            Message = message;
            Exchange = exchange;
            ExchangeType = exchangeType;
            Route = route;
            Headers = headers;
        }

        public T Message { get; }
        public string? Exchange { get; }
        public string? Route { get; }
        public ExchangeType ExchangeType { get; }
        public Headers Headers { get; }
        public int RetryAttemptNumber { get; }
    }
}
