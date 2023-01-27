# MassTransitRMQControllerExtensions
This is a wrapper over a standard MT library that works with RMQ. It allows you to make message consumers in a more "controller way".
This library uses attributes and some naming conventions to configure MT topology.
Net Standard 2.1
Main features:
1) Repeatable jobs configured with cron schedule(RunJob attribute) This is based on Quartz periodic signal emitter with a consumer on it.
2) Consumers configured with SubscribeOn attribute
3) Classic MS DI support. Supported through MT functionality 
4) Publish configuration through attributes on messages(PublishMessage attribute)
5) No MT behavior is changed.
6) Consumer execution results are logged by classic ILogger as a json string.
7) Easy to start new RMQ consumer/publisher project.  
8) Current retries are set for 6 hours(~20 minutes between tries)
9) Classic MT error handling.

Tips:

1) MTRMQExample project has examples to play around. 
2) rmqRun.txt file has the quickstart command for RMQ deployment in docker.
3) Message sending topology is resolved upon consumer app startup. 
4) Use RMQ Management for sending messages to exchanges.
5) ConfigureMassTransitControllers extension seeks all '*name*Controller'(by default) classes with attributed methods and attributed publish messages. Then it configures MT. There are no runtime configurations beyond MT support.
5) Parallelism for consumer is configured by attribute.
6) Use MT docs.
