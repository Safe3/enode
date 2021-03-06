﻿using System;
using ECommon.Utilities;
using ENode.Infrastructure;

namespace ENode.Eventing
{
    /// <summary>Represents an abstract base domain event.
    /// </summary>
    [Serializable]
    public abstract class DomainEvent<TAggregateRootId> : IDomainEvent
    {
        private string _aggregateRootId;
        private int? _version;
        private DateTime? _timestamp;

        /// <summary>Parameterized constructor.
        /// </summary>
        public DomainEvent(TAggregateRootId aggregateRootId)
        {
            if (aggregateRootId == null)
            {
                throw new ArgumentNullException("aggregateRootId");
            }
            Id = ObjectId.GenerateNewStringId();
            AggregateRootId = aggregateRootId;
            _aggregateRootId = aggregateRootId.ToString();
        }

        /// <summary>Represents the source aggregateRootId of the domain event.
        /// </summary>
        public TAggregateRootId AggregateRootId { get; private set; }
        /// <summary>Represents the unique id of the domain event.
        /// </summary>
        public string Id { get; private set; }
        /// <summary>Represents the version of the domain event, This property only should be set by framework.
        /// </summary>
        public int Version
        {
            get
            {
                return _version == null ? -1 : _version.Value;
            }
            set
            {
                if (_version != null)
                {
                    throw new ENodeException("The version of domain event cannot be set twice.");
                }
                _version = value;
            }
        }
        /// <summary>Represents the time of when this domain event raised.
        /// </summary>
        public DateTime Timestamp
        {
            get
            {
                return _timestamp == null ? DateTime.Now : _timestamp.Value;
            }
            set
            {
                if (_timestamp != null)
                {
                    throw new ENodeException("The timestamp of domain event cannot be set twice.");
                }
                _timestamp = value;
            }
        }

        /// <summary>Represents the unique id of the aggregate root, this property is only used by framework.
        /// </summary>
        string IDomainEvent.AggregateRootId
        {
            get
            {
                if (_aggregateRootId == null && AggregateRootId != null)
                {
                    _aggregateRootId = AggregateRootId.ToString();
                }
                return _aggregateRootId;
            }
        }
    }
}
