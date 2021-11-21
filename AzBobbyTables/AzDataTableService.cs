using Azure;
using Azure.Data.Tables;
using System;
using System.Collections;
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
        /// <param name="tableEndpoint">The table endpoint of the storage account.</param>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="storageAccountKey">The access key to the storage account.</param>
        internal static void Connect(Uri tableEndpoint, string tableName, string storageAccountKey)
        {
            var tableUriBuilder = new TableUriBuilder(tableEndpoint);
            var accountName = tableUriBuilder.AccountName;
            
            tableClient = new TableClient(tableEndpoint, tableName, new TableSharedKeyCredential(accountName, storageAccountKey));
        }

        /// <summary>
        /// Create a connection to the table using a shared access signature.
        /// </summary>
        /// <param name="sas">The SAS uri.</param>
        internal static void Connect(Uri sas)
        {
            tableClient = new TableClient(sas);
        }

        /// <summary>
        /// Remove one entity from a table.
        /// </summary>
        /// <param name="partitionKey">The partition key of the entity.</param>
        /// <param name="rowKey">The row key of the entity.</param>
        internal static void RemoveEntityFromTable(string partitionKey, string rowKey)
        {
            tableClient.DeleteEntity(partitionKey, rowKey);
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
        /// Add an entity to a table.
        /// </summary>
        /// <param name="psobject">The entity to add.</param>
        internal static void AddEntityToTable(PSObject psobject)
        {
            var entity = new TableEntity();
            foreach (var property in psobject.Properties)
            {
                entity.Add(property.Name, property.Value);
            }

            tableClient.AddEntity(entity);
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
                foreach (var property in r.Properties)
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
        /// Updates an entity in a table.
        /// </summary>
        /// <param name="psobject">The entity to update.</param>
        /// <returns>The result of the transaction.</returns>
        internal static Response UpdateEntityInTable(PSObject psobject)
        {
            TableEntity entity = new TableEntity();
            foreach (var property in psobject.Properties)
            {
                entity.Add(property.Name, property.Value);
            }

            return tableClient.UpdateEntity(entity, ETag.All);
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
                foreach (var property in r.Properties)
                {
                    entity.Add(property.Name, property.Value);
                }
                return entity;
            });

            transactions.AddRange(entities.Select(e => new TableTransactionAction(TableTransactionActionType.UpdateMerge, e)));

            return tableClient.SubmitTransaction(transactions);
        }

        /// <summary>
        /// Get an entity from a table.
        /// </summary>
        /// <param name="partitionKey">The partition key of the entity.</param>
        /// <param name="rowKey">The row key of the entity.</param>
        /// <returns>The entity if found.</returns>
        internal static Response<TableEntity> GetEntityFromTable(string partitionKey, string rowKey)
        {
            return tableClient.GetEntity<TableEntity>(partitionKey, rowKey);
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
