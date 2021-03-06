﻿using System;
using System.Data.SqlClient;
using System.Linq;
using ECommon.Components;
using ECommon.Dapper;
using ECommon.Serializing;

namespace ENode.Commanding.Impl
{
    /// <summary>The Microsoft SqlServer based implementation of ICommandStore.
    /// </summary>
    public class SqlServerCommandStore : ICommandStore
    {
        #region Private Variables

        private readonly string _connectionString;
        private readonly string _commandTable;
        private readonly string _primaryKeyName;
        private readonly IBinarySerializer _binarySerializer;
        private readonly ICommandTypeCodeProvider _commandTypeCodeProvider;

        #region Constructors

        /// <summary>Parameterized constructor.
        /// </summary>
        public SqlServerCommandStore(string connectionString, string commandTable, string primaryKeyName)
        {
            _connectionString = connectionString;
            _commandTable = commandTable;
            _primaryKeyName = primaryKeyName;
            _binarySerializer = ObjectContainer.Resolve<IBinarySerializer>();
            _commandTypeCodeProvider = ObjectContainer.Resolve<ICommandTypeCodeProvider>();
        }

        #endregion

        #endregion

        #region Public Methods

        public CommandAddResult AddCommand(HandledCommand handledCommand)
        {
            var record = ConvertTo(handledCommand);

            using (var connection = GetConnection())
            {
                connection.Open();
                try
                {
                    connection.Insert(record, _commandTable);
                    return CommandAddResult.Success;
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 2627)
                    {
                        if (ex.Message.Contains(_primaryKeyName))
                        {
                            return CommandAddResult.DuplicateCommand;
                        }
                    }
                    throw;
                }
            }
        }
        public HandledCommand Find(string commandId)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                var record = connection.QueryList<CommandRecord>(new { CommandId = commandId }, _commandTable).SingleOrDefault();
                if (record != null)
                {
                    return ConvertFrom(record);
                }
                return null;
            }
        }
        public void Remove(string commandId)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                connection.Delete(new { CommandId = commandId }, _commandTable);
            }
        }

        #endregion

        #region Private Methods

        private SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
        private HandledCommand ConvertFrom(CommandRecord record)
        {
            return new HandledCommand(
                _binarySerializer.Deserialize<ICommand>(record.Payload),
                record.SourceEventId,
                record.AggregateRootId,
                record.AggregateRootTypeCode);
        }
        private CommandRecord ConvertTo(HandledCommand handledCommand)
        {
            return new CommandRecord
            {
                CommandId = handledCommand.Command.Id,
                CommandTypeCode = _commandTypeCodeProvider.GetTypeCode(handledCommand.Command.GetType()),
                AggregateRootId = handledCommand.AggregateRootId,
                AggregateRootTypeCode = handledCommand.AggregateRootTypeCode,
                ProcessId = handledCommand.Command is IProcessCommand ? ((IProcessCommand)handledCommand.Command).ProcessId : null,
                SourceEventId = handledCommand.SourceEventId,
                Timestamp = DateTime.Now,
                Payload = _binarySerializer.Serialize(handledCommand.Command),
                Items = _binarySerializer.Serialize(handledCommand.Command.Items)
            };
        }

        #endregion

        class CommandRecord
        {
            public string CommandId { get; set; }
            public int CommandTypeCode { get; set; }
            public int AggregateRootTypeCode { get; set; }
            public string AggregateRootId { get; set; }
            public string ProcessId { get; set; }
            public string SourceEventId { get; set; }
            public DateTime Timestamp { get; set; }
            public byte[] Payload { get; set; }
            public byte[] Items { get; set; }
        }
    }
}
