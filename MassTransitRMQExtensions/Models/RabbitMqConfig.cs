using System;

namespace MassTransitRMQExtensions.Models
{
    public class RabbitMqConfig
    {
        public RabbitMqConfig(string userName, string password, Uri host)
        {
            UserName = userName;
            Password = password;
            Host = host;
        }

        public string UserName { get; set; }
        public string Password { get; set; }
        public Uri Host { get; set; }
    }
}
