using MassTransitRMQExtensions.Attributes;
using MassTransitRMQExtensions.Enums;
using Microsoft.Extensions.Logging;
using MTRMQExample.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MTRMQExample.Controllers
{
    public class TestController
    {
        public TestController(ILogger<TestController> logger)
        {
            this.Logger = logger;
        }

        public ILogger<TestController> Logger { get; }

        //[SubscribeOn("outerStatuses", ExchangeType.Topic, "101")]
        //[SubscribeTopicOn("outerStatuses", "101")]
        //[SubscribeTopicOn("outerStatuses", "102")]
        //[SubscribeOn("outerStatuses", ExchangeType.Topic, "#")]
        public Task<List<string>> Consume1(IEnumerable<JsonText> events)
        {
            //return $"{nameof(Consume1)}_result";
            return Task.FromResult(new List<string>() { $"{nameof(Consume1)}_result" });
            //return Task.FromResult(new List<string>() { $"{nameof(Consume1)}_result" });
            //return Task.CompletedTask;
            //return null;
            //return new object();
        }

        [SubscribeOn("outerStatusesV2", ExchangeType.Direct, "101")]
        [SubscribeDirectOn("outerStatusesV2", "102")]
        [SubscribeOn("outerStatusesV2", ExchangeType.Direct, "#")]
        public Task<List<string>> Consume2(IEnumerable<JsonText> events)
        {
            throw new System.Exception("Errr!!!");
            return Task.FromResult(new List<string>() { $"{nameof(Consume2)}_result" });
        }

        //[SubscribeTopicOn("outerStatusesV2")]
        public Task<List<string>> ConsumeV2RouteAll(IEnumerable<JsonText> events)
        {
            return Task.FromResult(new List<string>() { $"{nameof(ConsumeV2RouteAll)}_result" });
        }

        //[SubscribeTopicOn("outerStatusesV2", "101")]
        public Task<List<string>> ConsumeV2Route101(IEnumerable<JsonText> events)
        {
            return Task.FromResult(new List<string>() { $"{nameof(ConsumeV2Route101)}_result" });
        }

        //[SubscribeTopicOn("outerStatusesV2", "102")]
        public Task<List<string>> ConsumeV2Route102(IEnumerable<JsonText> events)
        {
            return Task.FromResult(new List<string>() { $"{nameof(ConsumeV2Route102)}_result" });
        }
    }
}
