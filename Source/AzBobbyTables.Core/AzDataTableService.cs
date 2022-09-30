using Azure;
using Azure.Data.Tables;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PipeHow.AzBobbyTables.Core
{
    public class AzDataTableService
    {
        private TableClient tableClient;
        public static string[] SupportedTypeList { get; } = {
            "byte[]",
            "bool",
            "boolean",
            "datetime",
            "datetimeoffset",
            "double",
            "guid",
            "int32",
            "int",
            "int64",
            "long",
            "string"
        };

        private AzDataTableService() { }

        public static AzDataTableService CreateWithConnectionString(string connectionString, string tableName, bool createIfNotExists)
        {
            var bobbyService = new AzDataTableService();
            var tableClient = new TableClient(connectionString, tableName);

            if (createIfNotExists)
            {
                tableClient.CreateIfNotExists();
            }

            bobbyService.tableClient = tableClient;
            return bobbyService;
        }

        public static AzDataTableService CreateWithStorageKey(string storageAccountName, string tableName, string storageAccountKey, bool createIfNotExists)
        {
            var bobbyService = new AzDataTableService();
            var tableEndpoint = new Uri($"https://{storageAccountName}.table.core.windows.net/{tableName}");
            var tableClient = new TableClient(tableEndpoint, tableName, new TableSharedKeyCredential(storageAccountName, storageAccountKey));

            if (createIfNotExists)
            {
                tableClient.CreateIfNotExists();
            }

            bobbyService.tableClient = tableClient;
            return bobbyService;
        }

        public static AzDataTableService CreateWithToken(string storageAccountName, string tableName, string token, bool createIfNotExists)
        {
            var bobbyService = new AzDataTableService();
            var tableEndpoint = new Uri($"https://{storageAccountName}.table.core.windows.net/{tableName}");
            var tableClient = new TableClient(tableEndpoint, tableName, new ExternalTokenCredential(token, DateTimeOffset.Now.Add(TimeSpan.FromHours(1))));

            if (createIfNotExists)
            {
                tableClient.CreateIfNotExists();
            }

            bobbyService.tableClient = tableClient;
            return bobbyService;
        }


        public static AzDataTableService CreateWithSAS(Uri sasUrl, string tableName, bool createIfNotExists)
        {
            var bobbyService = new AzDataTableService();
            // The credential is built only using the token
            var sasCredential = new AzureSasCredential(sasUrl.Query);

            // If the user did not specify a full endpoint to the table
            if (!sasUrl.ToString().Contains($"/{tableName}?"))
            {
                // Insert the table name before the URL parameters
                var urlParts = sasUrl.ToString().Split('?');
                sasUrl = new Uri($"{urlParts.First()}{tableName}?{urlParts.Last()}");
            }
            var tableClient = new TableClient(sasUrl, sasCredential);

            if (createIfNotExists)
            {
                tableClient.CreateIfNotExists();
            }

            bobbyService.tableClient = tableClient;
            return bobbyService;
        }

        /// <summary>
        /// Remove one or more entities from a table.
        /// </summary>
        /// <param name="hashtables">The list of entities to remove, with PartitionKey and RowKey set.</param>
        /// <returns>The result of the transaction.</returns>
        public void RemoveEntitiesFromTable(IEnumerable<Hashtable> hashtables)
        {
            var transactions = new List<TableTransactionAction>();

            var entities = hashtables.Select(r =>
            {
                return new TableEntity(r["PartitionKey"].ToString(), r["RowKey"].ToString());
            });

            transactions.AddRange(entities.Select(e => new TableTransactionAction(TableTransactionActionType.Delete, e)));

            SubmitTransaction(transactions);
        }

        /// <summary>
        /// Add one or more entities to a table.
        /// </summary>
        /// <param name="hashtables">The entities to add.</param>
        /// <param name="overwrite">Whether or not to update already existing entities.</param>
        /// <returns>The result of the transaction.</returns>
        public void AddEntitiesToTable(IEnumerable<Hashtable> hashtables, bool overwrite = false)
        {
            var transactions = new List<TableTransactionAction>();

            var entities = hashtables.Select(e =>
            {
                TableEntity entity = new TableEntity();
                foreach (string key in e.Keys)
                {
                    if (key != "ETag" && key != "Timestamp")
                    {
                        entity.Add(key, e[key]);
                    }
                }
                return entity;
            });

            TableTransactionActionType type = overwrite ? TableTransactionActionType.UpsertReplace : TableTransactionActionType.Add;
            transactions.AddRange(entities.Select(e => new TableTransactionAction(type, e)));
            
            SubmitTransaction(transactions);
        }

        /// <summary>
        /// Updates one or more entities in a table.
        /// </summary>
        /// <param name="hashtables">The entities to update.</param>
        /// <returns>The result of the transaction.</returns>
        public void UpdateEntitiesInTable(Hashtable[] hashtables)
        {
            var transactions = new List<TableTransactionAction>();

            var entities = hashtables.Select(e =>
            {
                TableEntity entity = new TableEntity();
                foreach (string key in e.Keys)
                {
                    if (key != "ETag" && key != "Timestamp")
                    {
                        entity.Add(key, e[key]);
                    }
                }
                return entity;
            });

            transactions.AddRange(entities.Select(e => new TableTransactionAction(TableTransactionActionType.UpdateMerge, e)));

            SubmitTransaction(transactions);
        }

        /// <summary>
        /// Submit transactions with built-in batch handling and splitting of partitions.
        /// </summary>
        private void SubmitTransaction(IList<TableTransactionAction> transactions)
        {
            // Transactions only support up to 100 entities of the same partitionkey
            // Loop through transactions grouped by partitionkey
            foreach (var group in transactions.GroupBy(t => t.Entity.PartitionKey))
            {
                // Loop through each group and submit up to 100 at a time
                for (int i = 0; i < group.Count(); i += 100)
                {
                    tableClient.SubmitTransaction(group.Skip(i).Take(100));
                }
            }
        }

        /// <summary>
        /// Get entities from a table based on a OData query.
        /// </summary>
        /// <param name="query">The query to filter entities by.</param>
        /// <param name="query">The list of properties to return.</param>
        /// <returns>The result of the query.</returns>
        public IEnumerable<Hashtable> GetEntitiesFromTable(string query, string[] properties = null)
        {
            // Get entities from table, loop through them and output them as hashtables
            // We cannot output the result as TableEntity objects, since we dont (want to) expose the SDK assembly to the user session
            return tableClient.Query<TableEntity>(query, select: properties).Select(e =>
            {
                Hashtable entityObject = new Hashtable();
                entityObject.Add("ETag", e["odata.etag"]);
                foreach (var key in e.Keys)
                {
                    if (key == "odata.etag") continue;
                    entityObject.Add(key, e[key]);
                }
                return entityObject;
            });
        }

        /// <summary>
        /// Clear all entities from a table.
        /// </summary>
        public void ClearTable()
        {
            var entities = tableClient.Query<TableEntity>((string)null, null, new[] { "PartitionKey", "RowKey" });

            var transactions = new List<TableTransactionAction>();

            transactions.AddRange(entities.Select(e => new TableTransactionAction(TableTransactionActionType.Delete, e)));

            SubmitTransaction(transactions);
        }
    }
}
