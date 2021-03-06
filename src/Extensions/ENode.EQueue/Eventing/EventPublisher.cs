﻿using System.Collections.Generic;
using System.Linq;
using ECommon.Components;
using ECommon.Logging;
using ECommon.Serializing;
using ENode.Eventing;
using ENode.Infrastructure;
using EQueue.Clients.Producers;
using EQueue.Protocols;

namespace ENode.EQueue
{
    public class EventPublisher : IEventPublisher
    {
        private const string DefaultEventPublisherProcuderId = "sys_epp";
        private readonly ILogger _logger;
        private readonly IBinarySerializer _binarySerializer;
        private readonly ITopicProvider<IDomainEvent> _eventTopicProvider;
        private readonly Producer _producer;

        public Producer Producer { get { return _producer; } }

        public EventPublisher(string id = null, ProducerSetting setting = null)
        {
            _producer = new Producer(id ?? DefaultEventPublisherProcuderId, setting ?? new ProducerSetting());
            _binarySerializer = ObjectContainer.Resolve<IBinarySerializer>();
            _eventTopicProvider = ObjectContainer.Resolve<ITopicProvider<IDomainEvent>>();
            _logger = ObjectContainer.Resolve<ILoggerFactory>().Create(GetType().FullName);
        }

        public EventPublisher Start()
        {
            _producer.Start();
            return this;
        }
        public EventPublisher Shutdown()
        {
            _producer.Shutdown();
            return this;
        }

        public void PublishEvent(IDictionary<string, string> contextItems, EventStream eventStream)
        {
            var eventMessage = ConvertToData(contextItems, eventStream);
            var topic = _eventTopicProvider.GetTopic(eventStream.Events.First());
            var data = _binarySerializer.Serialize(eventMessage);
            var message = new Message(topic, data);
            var result = _producer.Send(message, eventStream.AggregateRootId);
            if (result.SendStatus != SendStatus.Success)
            {
                throw new ENodeException("Publish event failed, eventStream:[{0}]", eventStream);
            }
        }

        private EventMessage ConvertToData(IDictionary<string, string> contextItems, EventStream eventStream)
        {
            var data = new EventMessage();

            data.CommandId = eventStream.CommandId;
            data.AggregateRootId = eventStream.AggregateRootId;
            data.AggregateRootTypeCode = eventStream.AggregateRootTypeCode;
            data.Timestamp = eventStream.Timestamp;
            data.ProcessId = eventStream.ProcessId;
            data.Version = eventStream.Version;
            data.Items = eventStream.Items;
            data.Events = eventStream.Events;
            data.ContextItems = contextItems;

            return data;
        }
    }
}
