using MassTransit;
using MassTransitRMQExtensions;
using MassTransitRMQExtensions.Attributes.ConsumerAttributes;
using MassTransitRMQExtensions.Attributes.JobAttributes;
using MassTransitRMQExtensions.Enums;
using MassTransitRMQExtensions.Models;
using Microsoft.Extensions.Logging;
using MTRMQExample.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MTRMQExample.Controllers
{
    public class TestController
    {
        public TestController(ILogger<TestController> logger, IPublishEndpoint publishEndpoint, ISendEndpointProvider sendEndpointProvider)
        {
            Logger = logger;
            //recommended message publisher
            PublishEndpoint = publishEndpoint;
            //sendEndpoint is not expected to work properly with exchange types other than fanout
            SendEndpointProvider = sendEndpointProvider;
        }

        public ILogger<TestController> Logger { get; }
        public IPublishEndpoint PublishEndpoint { get; }
        public ISendEndpointProvider SendEndpointProvider { get; }

        [SubscribeOn("outerStatuses", ExchangeType.Topic, "101", retryPolicy: "rmq intvl 3 0.5")]
        [SubscribeTopicOn("outerStatuses", "101", retryPolicy: "rmq intvl 3 0.5")]
        [SubscribeTopicOn("outerStatuses", "102", retryPolicy: "n")]
        [SubscribeOn("outerStatuses", ExchangeType.Topic, "#", retryPolicy: "mt exp 2 0.5 1 0.5")]
        //array message consume supported
        public Task<List<string>> Consume1(List<JsonText> events)
        {
            if (events.Count == 1)
            {
                throw new Exception("Grrrr!!!!!");
            }
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
        //custom event context with metadata like exchange, headers, route...
        public Task<List<string>> Consume2(MsgContext<IEnumerable<JsonText>> events)
        {
            throw new Exception("Errr!!!");
            //return Task.FromResult(new List<string>() { $"{nameof(Consume2)}_result" });
        }

        [SubscribeTopicOn("outerStatusesV2")]
        public Task<List<string>> ConsumeV2RouteAll(IEnumerable<JsonText> events)
        {
            return Task.FromResult(new List<string>() { $"{nameof(ConsumeV2RouteAll)}_result" });
        }
        //ConcurrentMessageLimit same parameter as in MT
        [SubscribeTopicOn("outerStatusesV2", "101", 5)]
        public Task<List<string>> ConsumeV2Route101(IEnumerable<JsonText> events)
        {
            return Task.FromResult(new List<string>() { $"{nameof(ConsumeV2Route101)}_result" });
        }

        [SubscribeTopicOn("outerStatusesV4", "101")]
        public async Task<string> ConsumeV4Route101(Message events)
        {
            return await Task.FromResult("result");
        }

        //equals to  [SubscribeOn(null, ExchangeType.Fanout)]
        [SubscribeEndpoint()]
        public async Task ConsumerWithoutBoundExchange(JsonText events)
        {
            //publish without binding. Not working with anonymous types. Supports only fanout exchanges.
            var endpoint = await SendEndpointProvider.GetSendEndpoint(new Uri("queue:SendQueue"));
            await endpoint.Send<AttributelessSendMessage>(new { Id = 56 });
            await Task.CompletedTask;
        }

        [RunJob("0/30 * * * * ?")]
        public async Task EventPublishExample()
        {
            //recommended extension for exchange publish
            //single event puslish example
            //use only the event type. object is not supported by MT, event base class will fail as well
            await PublishEndpoint.PublishMessage(new Message
            {
                Id = "asdfdf",
                Text = "text"
            }, "101");
        }

        [RunJob("0/30 * * * * ?")]
        public async Task EventsArrayPublishExample()
        {
            //events array puslish example
            await PublishEndpoint.PublishMessage(new ListMessage
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