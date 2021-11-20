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
        private static TableClient tableClient;

        internal static void Connect(string connectionString, string tableName)
        {
            tableClient = new TableClient(connectionString, tableName);
        }

        internal static void Connect(Uri tableEndpoint, string tableName, string storageAccountKey)
        {
            var tableUriBuilder = new TableUriBuilder(tableEndpoint);
            var accountName = tableUriBuilder.AccountName;
            
            tableClient = new TableClient(tableEndpoint, tableName, new TableSharedKeyCredential(accountName, storageAccountKey));
        }

        internal static void Connect(Uri sas)
        {
            tableClient = new TableClient(sas);
        }

        internal static void RemoveEntityFromTable(string partitionKey, string rowKey)
        {
            tableClient.DeleteEntity(partitionKey, rowKey);
        }

        internal static Response<IReadOnlyList<Response>> RemoveEntitiesFromTable(IEnumerable<Hashtable> hashtables)
        {
            var transactions = new List<TableTransactionAction>();

            var entities = hashtables.Select(r =>
            {
                return new TableEntity(r["PartitionKey"].ToString(), r["RowKey"].ToString());
            });

            transactions.AddRange(entities.Select(e => new TableTransactionAction(TableTransactionActionType.Delete, e)));

            return tableClient.SubmitTransaction(transactions);
        }

        internal static void AddEntityToTable(Hashtable hashtable)
        {
            var transactions = new List<TableTransactionAction>();

            var entity = new TableEntity();
            foreach (var key in hashtable.Keys)
            {
                entity.Add(key.ToString(), hashtable[key]);
            }

            tableClient.AddEntity(entity);
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
            
            return tableClient.SubmitTransaction(transactions);
        }

        internal static Response UpdateEntityInTable(Hashtable hashtable)
        {
            TableEntity entity = new TableEntity();
            foreach (var key in hashtable.Keys)
            {
                entity.Add(key.ToString(), hashtable[key]);
            }

            return tableClient.UpdateEntity(entity, ETag.All);
        }

        internal static Response<IReadOnlyList<Response>> UpdateEntitiesInTable(Hashtable[] hashtables)
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

            transactions.AddRange(entities.Select(e => new TableTransactionAction(TableTransactionActionType.UpdateMerge, e)));

            return tableClient.SubmitTransaction(transactions);
        }

        internal static Response<TableEntity> GetEntityFromTable(string partitionKey, string rowKey)
        {
            return tableClient.GetEntity<TableEntity>(partitionKey, rowKey);
        }

        internal static IList<TableEntity> GetEntitiesFromTable(string filter)
        {
            return tableClient.Query<TableEntity>(filter).ToList();
        }
    }
}
