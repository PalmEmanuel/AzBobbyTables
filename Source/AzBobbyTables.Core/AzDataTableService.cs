using Azure;
using Azure.Data.Tables;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PipeHow.AzBobbyTables.Core
{
    public static class AzDataTableService
    {
        private static TableClient tableClient;
        public static string[] SupportedTypeList { get; } = {
            "byte[]",
            "bool",
            "datetime",
            "double",
            "guid",
            "int32",
            "int",
            "int64",
            "long",
            "string"
        };

        /// <summary>
        /// Create a connection to the table using a connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to the storage account.</param>
        /// <param name="tableName">The name of the table.</param>
        public static void Connect(string connectionString, string tableName)
        {
            tableClient = new TableClient(connectionString, tableName);
        }

        /// <summary>
        /// Create a connection to the table using a storage account access key.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="storageAccountName">The name of the storage account.</param>
        /// <param name="storageAccountKey">The access key of the storage account.</param>
        public static void Connect(string storageAccountName, string tableName, string storageAccountKey)
        {
            var tableEndpoint = new Uri($"https://{storageAccountName}.table.core.windows.net/{tableName}");
            tableClient = new TableClient(tableEndpoint, tableName, new TableSharedKeyCredential(storageAccountName, storageAccountKey));
        }

        /// <summary>
        /// Create a connection to the table using a shared access signature.
        /// </summary>
        /// <param name="sasUrl">The table service SAS URL, with or without the table name.</param>
        /// <param name="tableName">The table name.</param>
        public static void Connect(Uri sasUrl, string tableName)
        {
            // The credential is built only using the token
            var sasCredential = new AzureSasCredential(sasUrl.Query);
            
            // If the user did not specify a full endpoint to the table
            if (!sasUrl.ToString().Contains($"/{tableName}?"))
            {
                // Insert the table name before the URL parameters
                var urlParts = sasUrl.ToString().Split('?');
                sasUrl = new Uri($"{urlParts.First()}{tableName}?{urlParts.Last()}");
            }
            tableClient = new TableClient(sasUrl, sasCredential);
        }

        /// <summary>
        /// Remove one or more entities from a table.
        /// </summary>
        /// <param name="Hashtables">The list of entities to remove, with PartitionKey and RowKey set.</param>
        /// <returns>The result of the transaction.</returns>
        public static void RemoveEntitiesFromTable(IEnumerable<Hashtable> Hashtables)
        {
            var transactions = new List<TableTransactionAction>();

            var entities = Hashtables.Select(r =>
            {
                return new TableEntity(r["PartitionKey"].ToString(), r["RowKey"].ToString());
            });

            transactions.AddRange(entities.Select(e => new TableTransactionAction(TableTransactionActionType.Delete, e)));

            tableClient.SubmitTransaction(transactions);
        }

        /// <summary>
        /// Add one or more entities to a table.
        /// </summary>
        /// <param name="Hashtables">The entities to add.</param>
        /// <param name="overwrite">Whether or not to update already existing entities.</param>
        /// <returns>The result of the transaction.</returns>
        public static void AddEntitiesToTable(IEnumerable<Hashtable> Hashtables, bool overwrite = false)
        {
            var transactions = new List<TableTransactionAction>();

            var entities = Hashtables.Select(e =>
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
            
            tableClient.SubmitTransaction(transactions);
        }

        /// <summary>
        /// Updates one or more entities in a table.
        /// </summary>
        /// <param name="Hashtables">The entities to update.</param>
        /// <returns>The result of the transaction.</returns>
        public static void UpdateEntitiesInTable(Hashtable[] Hashtables)
        {
            var transactions = new List<TableTransactionAction>();

            var entities = Hashtables.Select(e =>
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

            tableClient.SubmitTransaction(transactions);
        }

        /// <summary>
        /// Get entities from a table based on a OData query.
        /// </summary>
        /// <param name="query">The query to filter entities by.</param>
        /// <returns>The result of the query.</returns>
        public static IEnumerable<Hashtable> GetEntitiesFromTable(string query)
        {
            // Get all entities from table, loop through them and output them as PS(Custom)Objects
            // We cannot output the result as TableEntity objects, since we dont (want to) expose the SDK assembly to the user session
            return tableClient.Query<TableEntity>(query).Select(e =>
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
    }
}
