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

        
        [SubscribeOn("exchange", ExchangeType.Topic, "101")]
        [SubscribeTopicOn("exchange", "101")]
        [SubscribeTopicOn("exchange", "102")]
        [SubscribeOn("exchange", ExchangeType.Topic, "#")]
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
        
        [SubscribeOn("exchangeV3", ExchangeType.Direct, "101")]
        [SubscribeDirectOn("exchangeV3", "102")]
        [SubscribeOn("exchangeV3", ExchangeType.Direct, "#")]
        public Task<List<string>> Consume2(IEnumerable<JsonText> events)
        {
            throw new System.Exception($"Eror!!! First event batch Id: {events.FirstOrDefault()}");
            //return Task.FromResult(new List<string>() { $"{nameof(Consume2)}_result" });
        }

        [SubscribeTopicOn("exchangeV2")]
        public Task<List<string>> ConsumeV2RouteAll(IEnumerable<JsonText> events)
        {
            return Task.FromResult(new List<string>() { $"{nameof(ConsumeV2RouteAll)}_result" });
        }
        //ConcurrentMessageLimit same parameter as in MT
        [SubscribeTopicOn("exchangeV2", "101", 5)]
        public Task<List<string>> ConsumeV2Route101(IEnumerable<JsonText> events)
        {
            return Task.FromResult(new List<string>() { $"{nameof(ConsumeV2Route101)}_result" });
        }

        [SubscribeTopicOn("exchangeV4", "101")]
        public async Task<string> ConsumeV4Route101(Message events)
        {
            return await Task.FromResult("result");
        }

        [RunJob("0/30 * * * * ?")]
        public async Task EventPublishExample()
        {
            //recommended extension for exchange publish
            //single event publish example
            await this.PublishEndpoint.PublishMessage(new Message
            {
                Id = "asdfdf",
                Text = "text"
            }, "101");
        }

        [RunJob("0/30 * * * * ?")]
        public async Task EventsArrayPublishExample()
        {
            //events array publish example
            await this.PublishEndpoint.PublishMessage(new ListMessage
            {
                new UserMessage {
                Id = 987,
                Name = "SomeName"
                }
            }, "101");
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
