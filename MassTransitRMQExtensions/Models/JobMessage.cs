using System;
namespace MassTransitRMQExtensions.Models
{
    public class JobMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime DateTime { get; set; } = DateTime.Now;
    }
}
