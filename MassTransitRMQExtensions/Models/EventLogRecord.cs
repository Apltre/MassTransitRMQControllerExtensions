using System.Text.Json;
using System.Text.Json.Serialization;
using System;
using MassTransitRMQExtensions.Enums;
using System.Globalization;

namespace MassTransitRMQExtensions.Models
{
    public class EventLogRecord
    {
        public EventLogRecord(string exchange, string route, string queue, ExchangeType exchangeType, object @event)
        {
            this.Exchange = exchange;
            this.Route = route;
            this.Queue = queue;
            this.StartTime = DateTime.Now;
            this.Event = @event;
            this.MqTopologyType = exchangeType.ToString();
        }

        public bool IsSuccessfull { get; private set; }
        public string MqTopologyType { get; }
        public string Exchange { get; }
        public string Route { get; }
        public string Queue { get; }
        public DateTime StartTime { get; }
        public DateTime EndTime { get; private set; }
        public string Elapsed => (EndTime - StartTime).ToString("G", new CultureInfo("en-US"));
        public object Result { get; private set; }
        public object Event { get; }
        public Exception Exception { get; private set; }

        public void SetRecordEndState(DateTime endTime, bool isSuccessful = false, object result = null, Exception exception = null)
        {
            this.IsSuccessfull = isSuccessful;
            this.EndTime = endTime;
            this.Result = result;
            this.Exception = exception;
        }

        public override string ToString()
        {
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            return JsonSerializer.Serialize(this, options);
        }
    }
}