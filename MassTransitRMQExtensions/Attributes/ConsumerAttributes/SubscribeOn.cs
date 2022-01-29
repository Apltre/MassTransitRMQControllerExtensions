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
            Exchange = exchange;
            TopologyType = topologyType;
            Route = route;
        }

        public string Exchange { get; }
        public ExchangeType TopologyType { get; }
        public string Route { get; }

        private IEnumerable<ExchangeType> exchangeTypesWithoutRoutes = new List<ExchangeType> { ExchangeType.Fanout };

        private bool routeEquals(SubscribeOn other)
        {
            if (exchangeTypesWithoutRoutes.Contains(TopologyType))
            {
                return true;
            }
            return Route == other.Route;
        }

        public bool Equals(SubscribeOn other)
        {

            return Exchange == other.Exchange
             && TopologyType == other.TopologyType
             && routeEquals(other);
        }

        private int getRouteHash(string route)
        {
            if (exchangeTypesWithoutRoutes.Contains(TopologyType) || Route is null)
            {
                return 0.GetHashCode();
            }

            return Route.GetHashCode();
        }

        public override int GetHashCode()
        {
            int hashExchange = (Exchange ?? "").GetHashCode();
            int hashTopologyType = TopologyType.GetHashCode();
            int hashRoute = getRouteHash(Route ?? "");
            return hashExchange ^ hashTopologyType ^ hashRoute;
        }
    }
}
