using System;

namespace MassTransitRMQExtensions.Exceptions
{
    public class MTConsumerFailFastException : Exception
    {
        public MTConsumerFailFastException() { }
        public MTConsumerFailFastException(string message) : base(message) { }
    }
}