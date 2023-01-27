using System;

namespace MassTransitRMQExtensions.Converters.Dynamic
{
    public class UnsupportedPropertyTypeException: Exception
    {
        public UnsupportedPropertyTypeException(string message):base(message)
        {
        }
    }
}