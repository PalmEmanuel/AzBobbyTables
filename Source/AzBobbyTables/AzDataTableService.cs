using Azure;
using Azure.Data.Tables;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PipeHow.AzBobbyTables
{
    internal static class AzDataTableService
    {
        private static TableClient _tableClient;

        internal static void Connect(string connectionString, string tableName)
        {
            _tableClient = new TableClient(connectionString, tableName);
        }

        internal static void Connect(Uri sas)
        {
            _tableClient = new TableClient(sas);
        }

        internal static void RemoveEntityFromTable(string partitionKey, string rowKey)
        {
            _tableClient.DeleteEntity(partitionKey, rowKey);
        }

        internal static Response<IReadOnlyList<Response>> RemoveEntitiesFromTable(IEnumerable<Hashtable> hashtables)
        {
            var transactions = new List<TableTransactionAction>();

            var entities = hashtables.Select(r =>
            {
                return new TableEntity(r["PartitionKey"].ToString(), r["RowKey"].ToString());
            });

            transactions.AddRange(entities.Select(e => new TableTransactionAction(TableTransactionActionType.Delete, e)));

            return _tableClient.SubmitTransaction(transactions);
        }

        internal static void AddEntityToTable(Hashtable hashtable)
        {
            var transactions = new List<TableTransactionAction>();

            var entity = new TableEntity();
            foreach (var key in hashtable.Keys)
            {
                entity.Add(key.ToString(), hashtable[key]);
            }

            _tableClient.AddEntity(entity);
        }

        internal static Response<IReadOnlyList<Response>> AddEntitiesToTable(IEnumerable<Hashtable> hashtables, bool overwrite = false)
        {
            var transactions = new List<TableTransactionAction>();

            var entities = hashtables.Select(r =>
            {
                TableEntity entity = new TableEntity();
                foreach (var key in r.Keys)
                {
                    entity.Add(key.ToString(), r[key]);
                }
                return entity;
            });

            TableTransactionActionType type = overwrite ? TableTransactionActionType.UpsertReplace : TableTransactionActionType.Add;
            transactions.AddRange(entities.Select(e => new TableTransactionAction(type, e)));
            
            return _tableClient.SubmitTransaction(transactions);
        }
    }
}
