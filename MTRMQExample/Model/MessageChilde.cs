using MassTransitRMQExtensions.Attributes.PublishAttributes;
using MassTransitRMQExtensions.Enums;

namespace MTRMQExample.Model
{
    [PublishMessage(ExchangeType.Topic, "msg1")]
    public class MessageChilde : MessageBase
    {
    }
}
