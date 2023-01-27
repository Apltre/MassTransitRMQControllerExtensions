using MassTransitRMQExtensions.Attributes.PublishAttributes;
using MassTransitRMQExtensions.Enums;
using System.Collections.Generic;

namespace MTRMQExample.Model
{
    public class UserMessage
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    //attributes are mandatory for publish
    //events array publish made by List inheritance
    [PublishMessage(ExchangeType.Topic, "outerStatusesV5")]
    public class ListMessage : List<UserMessage>
    {

    }
}
