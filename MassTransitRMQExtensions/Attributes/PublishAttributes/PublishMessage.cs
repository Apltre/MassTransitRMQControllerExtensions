using MassTransitRMQExtensions.Enums;
using System;

namespace MassTransitRMQExtensions.Attributes.PublishAttributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class PublishMessage : Attribute
    {
        public PublishMessage(ExchangeType exchangeType, string exchangeName, bool resolveTopology = true)
        {
            this.ExchangeType = exchangeType;
            this.ExchangeName = exchangeName;
            this.ResolveTopology = resolveTopology;
        }

        public ExchangeType ExchangeType { get; }
        public string ExchangeName { get; }
        public bool ResolveTopology { get; }
    }
}