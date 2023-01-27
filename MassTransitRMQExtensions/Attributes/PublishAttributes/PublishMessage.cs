using MassTransitRMQExtensions.Enums;
using System;

namespace MassTransitRMQExtensions.Attributes.PublishAttributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class PublishMessage : Attribute
    {
        public PublishMessage(ExchangeType exchangeType, string exchangeName, bool resolveTopology = true)
        {
            ExchangeType = exchangeType;
            ExchangeName = exchangeName;
            ResolveTopology = resolveTopology;
        }

        public ExchangeType ExchangeType { get; }
        public string ExchangeName { get; }
        public bool ResolveTopology { get; }
    }
}