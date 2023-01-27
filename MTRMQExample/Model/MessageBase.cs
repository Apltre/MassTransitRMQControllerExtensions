using MassTransit;

namespace MTRMQExample.Model
{
    //use this attribute on events base class to evade useless/bad topology creation
    //don't inherit from types with !PublishMessage! attribute
    [ExcludeFromTopology]
    public class MessageBase
    {
        public int Id { get; set; }
    }
}