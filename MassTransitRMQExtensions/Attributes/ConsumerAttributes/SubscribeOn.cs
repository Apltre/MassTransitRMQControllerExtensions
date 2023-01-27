using MassTransitRMQExtensions.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MassTransitRMQExtensions.Attributes.ConsumerAttributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class SubscribeOn : Attribute, IEquatable<SubscribeOn>
    {
        public SubscribeOn(string? exchange, ExchangeType topologyType, string? route = null, int concurrentMessageLimit = 1, string? retryPolicy = null)
        {
            Exchange = exchange;
            TopologyType = topologyType;
            Route = route;
            ConcurrentMessageLimit = concurrentMessageLimit;
            RetryPolicy = retryPolicy;
        }

        public SubscribeOn(object? exchange, ExchangeType topologyType, string? route = null, int concurrentMessageLimit = 1, string? retryPolicy = null)
            : this(exchange?.ToString(), topologyType, route, concurrentMessageLimit, retryPolicy) { }


        public string? Exchange { get; }
        public ExchangeType TopologyType { get; }
        public string? Route { get; }
        public int ConcurrentMessageLimit { get; }
        public string? RetryPolicy { get; }

        private static readonly IEnumerable<ExchangeType> exchangeTypesWithoutRoutes = new List<ExchangeType> { ExchangeType.Fanout };

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
             && routeEquals(other)
             && RetryPolicy == other.RetryPolicy;
        }

        private int getRouteHash(string? route)
        {
            if (exchangeTypesWithoutRoutes.Contains(TopologyType) || route is null)
            {
                return 0.GetHashCode();
            }

            return route.GetHashCode();
        }

        public override int GetHashCode()
        {
            int hashExchange = (Exchange ?? "").GetHashCode();
            int hashTopologyType = TopologyType.GetHashCode();
            int hashRoute = getRouteHash(Route);
            return hashExchange ^ hashTopologyType ^ hashRoute;
        }
    }
}
