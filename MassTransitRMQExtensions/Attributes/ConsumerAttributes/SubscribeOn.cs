using MassTransitRMQExtensions.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MassTransitRMQExtensions.Attributes.ConsumerAttributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class SubscribeOn : Attribute, IEquatable<SubscribeOn>
    {
        public SubscribeOn(string exchange, ExchangeType topologyType, string route = "#")
        {
            this.Exchange = exchange;
            this.TopologyType = topologyType;
            this.Route = route;
        }

        public string Exchange { get; }
        public ExchangeType TopologyType { get; }
        public string Route { get; }

        private IEnumerable<ExchangeType> exchangeTypesWithoutRoutes = new List<ExchangeType> { ExchangeType.Fanout };

        private bool routeEquals(SubscribeOn other)
        {
            if (this.exchangeTypesWithoutRoutes.Contains(this.TopologyType))
            {
                return true;
            }
            return this.Route == other.Route;
        }

        public bool Equals(SubscribeOn other)
        {

            return this.Exchange == other.Exchange
             && this.TopologyType == other.TopologyType
             && this.routeEquals(other);
        }

        private int getRouteHash(string route)
        {
            if (this.exchangeTypesWithoutRoutes.Contains(this.TopologyType) || this.Route is null)
            {
                return 0.GetHashCode();
            }

            return this.Route.GetHashCode();
        }

        public override int GetHashCode()
        {
            int hashExchange = (this.Exchange ?? "").GetHashCode();
            int hashTopologyType = this.TopologyType.GetHashCode();
            int hashRoute = getRouteHash(this.Route ?? "");
            return hashExchange ^ hashTopologyType ^ hashRoute;
        }
    }
}
