using System;

namespace MassTransitRMQExtensions.Models
{
    public class RabbitMqConfig
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public Uri Host { get; set; }
    }
}
