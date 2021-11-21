using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables
{
    internal static class AzDataTableService
    {
        private static TableClient tableClient;

        /// <summary>
        /// Create a connection to the table using a connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to the storage account.</param>
        /// <param name="tableName">The name of the table.</param>
        internal static void Connect(string connectionString, string tableName)
        {
            tableClient = new TableClient(connectionString, tableName);
        }

        /// <summary>
        /// Create a connection to the table using a storage account access key.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="storageAccountName">The name of the storage account.</param>
        /// <param name="storageAccountKey">The access key of the storage account.</param>
        internal static void Connect(string storageAccountName, string tableName, string storageAccountKey)
        {
            var tableEndpoint = new Uri($"https://{storageAccountName}.table.core.windows.net/{tableName}");
            tableClient = new TableClient(tableEndpoint, tableName, new TableSharedKeyCredential(storageAccountName, storageAccountKey));
        }

        /// <summary>
        /// Create a connection to the table using a shared access signature.
        /// </summary>
        /// <param name="sasUrl">The table service SAS URL, with or without the table name.</param>
        /// <param name="tableName">The table name.</param>
        internal static void Connect(Uri sasUrl, string tableName)
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
        /// <param name="psobjects">The list of entities to remove, with PartitionKey and RowKey set.</param>
        /// <returns>The result of the transaction.</returns>
        internal static Response<IReadOnlyList<Response>> RemoveEntitiesFromTable(IEnumerable<PSObject> psobjects)
        {
            var transactions = new List<TableTransactionAction>();

            var entities = psobjects.Select(r =>
            {
                return new TableEntity(r.Properties["PartitionKey"].Value.ToString(), r.Properties["RowKey"].Value.ToString());
            });

            transactions.AddRange(entities.Select(e => new TableTransactionAction(TableTransactionActionType.Delete, e)));

            return tableClient.SubmitTransaction(transactions);
        }

        /// <summary>
        /// Add one or more entities to a table.
        /// </summary>
        /// <param name="psobjects">The entities to add.</param>
        /// <param name="overwrite">Whether or not to update already existing entities.</param>
        /// <returns>The result of the transaction.</returns>
        internal static Response<IReadOnlyList<Response>> AddEntitiesToTable(IEnumerable<PSObject> psobjects, bool overwrite = false)
        {
            var transactions = new List<TableTransactionAction>();

            var entities = psobjects.Select(r =>
            {
                TableEntity entity = new TableEntity();
                foreach (var property in r.Properties.Where(p => p.Name != "ETag" && p.Name != "Timestamp"))
                {
                    entity.Add(property.Name, property.Value);
                }
                return entity;
            });

            TableTransactionActionType type = overwrite ? TableTransactionActionType.UpsertReplace : TableTransactionActionType.Add;
            transactions.AddRange(entities.Select(e => new TableTransactionAction(type, e)));
            
            return tableClient.SubmitTransaction(transactions);
        }

        /// <summary>
        /// Updates one or more entities in a table.
        /// </summary>
        /// <param name="psobjects">The entities to update.</param>
        /// <returns>The result of the transaction.</returns>
        internal static Response<IReadOnlyList<Response>> UpdateEntitiesInTable(PSObject[] psobjects)
        {
            var transactions = new List<TableTransactionAction>();

            var entities = psobjects.Select(r =>
            {
                TableEntity entity = new TableEntity();
                foreach (var property in r.Properties.Where(p => p.Name != "ETag" && p.Name != "Timestamp"))
                {
                    entity.Add(property.Name, property.Value);
                }
                return entity;
            });

            transactions.AddRange(entities.Select(e => new TableTransactionAction(TableTransactionActionType.UpdateMerge, e)));

            return tableClient.SubmitTransaction(transactions);
        }

        /// <summary>
        /// Get entities from a table based on a OData query.
        /// </summary>
        /// <param name="query">The query to filter entities by.</param>
        /// <returns>The result of the query.</returns>
        internal static IList<TableEntity> GetEntitiesFromTable(string query)
        {
            return tableClient.Query<TableEntity>(query).ToList();
        }
    }
}
