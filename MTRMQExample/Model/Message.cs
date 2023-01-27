using MassTransitRMQExtensions.Attributes.PublishAttributes;
using MassTransitRMQExtensions.Enums;

namespace MassTransitRMQExtensions.Models
{
    //attributes are mandatory for publish
    [PublishMessage(ExchangeType.Topic, "outerStatusesV4")]
    public class Message
    {
        public  string Id { get; set; }
        public string Text { get; set; }
    }
}