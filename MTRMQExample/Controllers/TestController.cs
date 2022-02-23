using MassTransit;
using MassTransitRMQExtensions;
using MassTransitRMQExtensions.Attributes.ConsumerAttributes;
using MassTransitRMQExtensions.Attributes.JobAttributes;
using MassTransitRMQExtensions.Enums;
using MassTransitRMQExtensions.Models;
using Microsoft.Extensions.Logging;
using MTRMQExample.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MTRMQExample.Controllers
{
    public class TestController
    {
        public TestController(ILogger<TestController> logger, IPublishEndpoint publishEndpoint)
        {
            this.Logger = logger;
            //sendEndpoint is not expected to work properly with exchange types other than fanout
            this.PublishEndpoint = publishEndpoint;
        }

        public ILogger<TestController> Logger { get; }
        public IPublishEndpoint PublishEndpoint { get; }

        
        [SubscribeOn("outerStatuses", ExchangeType.Topic, "101")]
        [SubscribeTopicOn("outerStatuses", "101")]
        [SubscribeTopicOn("outerStatuses", "102")]
        [SubscribeOn("outerStatuses", ExchangeType.Topic, "#")]
        //array message consume supported
        public Task<List<string>> Consume1(IEnumerable<JsonText> events)
        {      
            //return $"{nameof(Consume1)}_result";
            return Task.FromResult(new List<string>() { $"{nameof(Consume1)}_result" });
            //return Task.FromResult(new List<string>() { $"{nameof(Consume1)}_result" });
            //return Task.CompletedTask;
            //return null;
            //return new object();
        }
        
        [SubscribeOn("outerStatusesV3", ExchangeType.Direct, "101")]
        [SubscribeDirectOn("outerStatusesV3", "102")]
        [SubscribeOn("outerStatusesV3", ExchangeType.Direct, "#")]
        public Task<List<string>> Consume2(IEnumerable<JsonText> events)
        {
            throw new System.Exception($"Eror!!! First event batch Id: {events.FirstOrDefault()}");
            //return Task.FromResult(new List<string>() { $"{nameof(Consume2)}_result" });
        }

        [SubscribeTopicOn("outerStatusesV2")]
        public Task<List<string>> ConsumeV2RouteAll(IEnumerable<JsonText> events)
        {
            return Task.FromResult(new List<string>() { $"{nameof(ConsumeV2RouteAll)}_result" });
        }

        [SubscribeTopicOn("outerStatusesV2", "101")]
        public Task<List<string>> ConsumeV2Route101(IEnumerable<JsonText> events)
        {
            return Task.FromResult(new List<string>() { $"{nameof(ConsumeV2Route101)}_result" });
        }

        //ConcurrentMessageLimit same parameter as in MT
        [SubscribeTopicOn("outerStatusesV2", "102", 5)]
        public async Task<List<string>> ConsumeV2Route102(IEnumerable<JsonText> events)
        {
            //recommended extension for exchange publish
            //array publish as message not supported
            await this.PublishEndpoint.PublishMessage(new Message
            {
                Id = "asdfdf",
                Text = "text"
            }, "101");
            return new List<string>() { $"{nameof(ConsumeV2Route102)}_result" };
        }

        [SubscribeTopicOn("outerStatusesV4", "101")]
        public async Task<string> ConsumeV4Route101(Message events)
        {
            return $"Result. Event Id: {events.Id}";
        }

        [RunJob("0/1 * * * * ?")]
        public async Task<string> Job1()
        {
            return await Task.FromResult($"{nameof(Job1)}_result");
        }
        
        [RunJob("0/5 * * * * ?")]
        public async Task<string> Job2()
        {
            return await Task.FromResult($"{nameof(Job2)}_result");
        }
    }
}
