using MassTransitRMQExtensions.Attributes.PublishAttributes;
using MassTransitRMQExtensions.Enums;

namespace MTRMQExample.Model
{
    [PublishMessage(ExchangeType.Topic, "msg2")]
    public class MessageChilde1 : MessageBase
    {
    }
}
