using System.Text.Json;
using System.Text.Json.Serialization;
using System;
using MassTransitRMQExtensions.Enums;
using System.Globalization;

namespace MassTransitRMQExtensions.Models
{
    public class MessageLogRecord
    {
        private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public MessageLogRecord(string? exchange, string? route, string queue, ExchangeType exchangeType, object? message, int? retryAttempt)
        {
            Exchange = exchange;
            Route = !string.IsNullOrWhiteSpace(route) ? route : null;
            Queue = queue;
            StartTime = DateTime.Now;
            Message = message is null ? null : JsonSerializer.Deserialize<object>(JsonSerializer.Serialize(message, jsonOptions));
            MqTopologyType = exchangeType.ToString();
            RetryAttempt = retryAttempt;
        }

        public bool IsSuccessful { get; private set; }
        public string MqTopologyType { get; }
        public string? Exchange { get; }
        public string? Route { get; }
        public string Queue { get; }
        public DateTime StartTime { get; }
        public DateTime EndTime { get; private set; }
        public string Elapsed => (EndTime - StartTime).ToString("G", new CultureInfo("en-US"));
        public object? Result { get; private set; }
        public object? Message { get; }
        public string? Exception { get; private set; }
        public int? RetryAttempt { get; private set; }

        public void SetRecordEndState(DateTime endTime, bool isSuccessful = false, object? result = null, Exception? exception = null)
        {
            IsSuccessful = isSuccessful;
            EndTime = endTime;
            Result = result;
            Exception = exception?.ToString();
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this, jsonOptions);
        }
    }
}