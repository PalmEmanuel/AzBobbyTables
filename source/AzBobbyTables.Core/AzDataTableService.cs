using Azure;
using Azure.Data.Tables;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading;

namespace PipeHow.AzBobbyTables.Core;

public class AzDataTableService
{
    private TableClient? TableClient { get; set; }

    /// <summary>
    /// Cancellation token used within the AzDataTableService.
    /// </summary>
    private CancellationToken CancellationToken { get; }

    /// <summary>
    /// List of supported data types for the table.
    /// </summary>
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

    private AzDataTableService(CancellationToken cancellationToken) => CancellationToken = cancellationToken;

    private static void CreateIfNotExists(TableClient client, CancellationToken cancellationToken)
    {
        try
        {
            client.CreateIfNotExists(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new AzDataTableException(new ErrorRecord(ex, "CreateTableError", ErrorCategory.InvalidOperation, null));
        }
    }

    public static AzDataTableService CreateWithConnectionString(string connectionString, string tableName, bool createIfNotExists, CancellationToken cancellationToken)
    {
        try
        {
            var dataTableService = new AzDataTableService(cancellationToken);
        
            TableClient client = new(connectionString, tableName);

            if (createIfNotExists)
            {
                CreateIfNotExists(client, cancellationToken);
            }

            dataTableService.TableClient = client;
            return dataTableService;
        }
        catch (Exception ex)
        {
            throw new AzDataTableException(new ErrorRecord(ex, "ConnectWithConnectionStringError", ErrorCategory.ConnectionError, null));
        }
    }

    public static AzDataTableService CreateWithStorageKey(string storageAccountName, string tableName, string storageAccountKey, bool createIfNotExists, CancellationToken cancellationToken)
    {
        try
        {
            var dataTableService = new AzDataTableService(cancellationToken);
            var tableEndpoint = new Uri($"https://{storageAccountName}.table.core.windows.net/{tableName}");
        
            TableClient client = new(tableEndpoint, tableName, new TableSharedKeyCredential(storageAccountName, storageAccountKey));

            if (createIfNotExists)
            {
                CreateIfNotExists(client, cancellationToken);
            }

            dataTableService.TableClient = client;
            return dataTableService;
        }
        catch (Exception ex)
        {
            throw new AzDataTableException(new ErrorRecord(ex, "ConnectWithStorageKeyError", ErrorCategory.ConnectionError, null));
        }
    }

    public static AzDataTableService CreateWithToken(string storageAccountName, string tableName, string token, bool createIfNotExists, CancellationToken cancellationToken)
    {
        try
        {
            var dataTableService = new AzDataTableService(cancellationToken);
            var tableEndpoint = new Uri($"https://{storageAccountName}.table.core.windows.net/{tableName}");

            TableClient client = new(tableEndpoint, tableName, new ExternalTokenCredential(token, DateTimeOffset.Now.Add(TimeSpan.FromHours(1))));

            if (createIfNotExists)
            {
                CreateIfNotExists(client, cancellationToken);
            }

            dataTableService.TableClient = client;
            return dataTableService;
        }
        catch (Exception ex)
        {
            throw new AzDataTableException(new ErrorRecord(ex, "ConnectWithTokenError", ErrorCategory.ConnectionError, null));
        }
    }

    public static AzDataTableService CreateWithSAS(Uri sasUrl, string tableName, bool createIfNotExists, CancellationToken cancellationToken)
    {
        try
        {
            var dataTableService = new AzDataTableService(cancellationToken);
            // The credential is built only using the token
            var sasCredential = new AzureSasCredential(sasUrl.Query);

            // If the user did not specify a full endpoint to the table
            if (!sasUrl.ToString().Contains($"/{tableName}?"))
            {
                // Insert the table name before the URL parameters
                var urlParts = sasUrl.ToString().Split('?');
                sasUrl = new Uri($"{urlParts.First()}{tableName}?{urlParts.Last()}");
            }
        
            TableClient client = new(sasUrl, sasCredential);

            if (createIfNotExists)
            {
                CreateIfNotExists(client, cancellationToken);
            }

            dataTableService.TableClient = client;
            return dataTableService;
        }
        catch (Exception ex)
        {
            throw new AzDataTableException(new ErrorRecord(ex, "ConnectWithSASError", ErrorCategory.ConnectionError, null));
        }
    }

    public void RemoveTable()
    {
        try
        {
            TableClient?.Delete();
        }
        catch (Exception ex)
        {
            throw new AzDataTableException(new ErrorRecord(ex, "DeleteTableError", ErrorCategory.InvalidOperation, null));
        }
    }

    /// <summary>
    /// Remove one or more entities from a table.
    /// </summary>
    /// <param name="hashtables">The list of entities to remove, with PartitionKey and RowKey set.</param>
    /// <returns>The result of the transaction.</returns>
    public void RemoveEntitiesFromTable(IEnumerable<Hashtable> hashtables)
    {
        try
        {
            var transactions = new List<TableTransactionAction>();

            var entities = hashtables.Select(r =>
            {
                return new TableEntity(r["PartitionKey"].ToString(), r["RowKey"].ToString());
            });

            transactions.AddRange(entities.Select(e => new TableTransactionAction(TableTransactionActionType.Delete, e)));

            SubmitTransaction(transactions);
        }
        catch (Exception ex)
        {
            throw new AzDataTableException(new ErrorRecord(ex, "RemoveEntitiesHashtableError", ErrorCategory.InvalidOperation, null));
        }
    }

    /// <summary>
    /// Remove one or more entities from a table.
    /// </summary>
    /// <param name="psobjects">The list of entities to remove, with PartitionKey and RowKey set.</param>
    /// <returns>The result of the transaction.</returns>
    public void RemoveEntitiesFromTable(IEnumerable<PSObject> psobjects)
    {
        try
        {
            var transactions = new List<TableTransactionAction>();

            var entities = psobjects.Select(e =>
            {
                return new TableEntity(
                    e.Properties.First(p => p.Name == "PartitionKey").Value.ToString(),
                    e.Properties.First(p => p.Name == "RowKey").Value.ToString()
                );
            });

            transactions.AddRange(entities.Select(e => new TableTransactionAction(TableTransactionActionType.Delete, e)));

            SubmitTransaction(transactions);
        }
        catch (Exception ex)
        {
            throw new AzDataTableException(new ErrorRecord(ex, "RemoveEntitiesPSObjectError", ErrorCategory.InvalidOperation, null));
        }
    }

    /// <summary>
    /// Add one or more entities to a table.
    /// </summary>
    /// <param name="hashtables">The entities to add.</param>
    /// <param name="overwrite">Whether or not to update already existing entities.</param>
    /// <returns>The result of the transaction.</returns>
    public void AddEntitiesToTable(IEnumerable<Hashtable> hashtables, bool overwrite = false)
    {
        try
        {
            var transactions = new List<TableTransactionAction>();

            var entities = hashtables.Select(e =>
            {
                TableEntity entity = new();
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
        catch (Exception ex)
        {
            throw new AzDataTableException(new ErrorRecord(ex, "AddEntitiesHashtableError", ErrorCategory.InvalidOperation, null));
        }
    }

    /// <summary>
    /// Add one or more entities to a table.
    /// </summary>
    /// <param name="psobjects">The entities to add.</param>
    /// <param name="overwrite">Whether or not to update already existing entities.</param>
    /// <returns>The result of the transaction.</returns>
    public void AddEntitiesToTable(IEnumerable<PSObject> psobjects, bool overwrite = false)
    {
        try
        {
            var transactions = new List<TableTransactionAction>();

            var entities = psobjects.Select(e =>
            {
                TableEntity entity = new();
                foreach (var prop in e.Properties)
                {
                    if (prop.Name != "ETag" && prop.Name != "Timestamp")
                    {
                        entity.Add(prop.Name, prop.Value);
                    }
                }
                return entity;
            });

            TableTransactionActionType type = overwrite ? TableTransactionActionType.UpsertReplace : TableTransactionActionType.Add;
            transactions.AddRange(entities.Select(e => new TableTransactionAction(type, e)));

            SubmitTransaction(transactions);
        }
        catch (Exception ex)
        {
            throw new AzDataTableException(new ErrorRecord(ex, "AddEntitiesPSObjectError", ErrorCategory.InvalidOperation, null));
        }
    }

    /// <summary>
    /// Updates one or more entities in a table.
    /// </summary>
    /// <param name="hashtables">The entities to update.</param>
    /// <returns>The result of the transaction.</returns>
    public void UpdateEntitiesInTable(IEnumerable<Hashtable> hashtables)
    {
        try
        {
            var transactions = new List<TableTransactionAction>();

            var entities = hashtables.Select(e =>
            {
                TableEntity entity = new();
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
        catch (Exception ex)
        {
            throw new AzDataTableException(new ErrorRecord(ex, "UpdateEntitiesHashtableError", ErrorCategory.InvalidOperation, null));
        }
    }

    /// <summary>
    /// Updates one or more entities in a table.
    /// </summary>
    /// <param name="psobjects">The entities to update.</param>
    /// <returns>The result of the transaction.</returns>
    public void UpdateEntitiesInTable(IEnumerable<PSObject> psobjects)
    {
        try
        {
            var transactions = new List<TableTransactionAction>();

            var entities = psobjects.Select(e =>
            {
                TableEntity entity = new();
                foreach (var prop in e.Properties)
                {
                    if (prop.Name != "ETag" && prop.Name != "Timestamp")
                    {
                        entity.Add(prop.Name, prop.Value);
                    }
                }
                return entity;
            });

            transactions.AddRange(entities.Select(e => new TableTransactionAction(TableTransactionActionType.UpdateMerge, e)));

            SubmitTransaction(transactions);
        }
        catch (Exception ex)
        {
            throw new AzDataTableException(new ErrorRecord(ex, "UpdateEntitiesPSObjectError", ErrorCategory.InvalidOperation, null));
        }
    }

    /// <summary>
    /// Get entities from a table based on a OData query.
    /// </summary>
    /// <param name="query">The query to filter entities by.</param>
    /// <param name="query">The list of properties to return.</param>
    /// <returns>The result of the query.</returns>
    public IEnumerable<PSObject> GetEntitiesFromTable(string query, string[] properties = null!, int? top = null, int? skip = null, string[] orderBy = null!)
    {
        try
        {
            // Declare type as IAsyncEnumerable to be able to overwrite it with LINQ results further down
            IAsyncEnumerable<TableEntity> entities = TableClient!.QueryAsync<TableEntity>(query, null, properties, CancellationToken);

            // If user specified one or more properties to sort list by
            // This may slow the query down a lot with a lot of results
            if (orderBy is not null && orderBy.Any())
            {
                // Must create a new variable to be able to modify it within the loop
                // https://ericlippert.com/2009/11/12/closing-over-the-loop-variable-considered-harmful-part-one/
                // OrderBy for the first property, ThenBy for the rest
                var orderableEntities = entities.OrderBy(e => e[orderBy.First()]);
                foreach (var propertyName in orderBy.Skip(1))
                {
                    orderableEntities = orderableEntities.ThenBy(e => e[propertyName]);
                }
                entities = orderableEntities;
            }
            // If user specified to skip a number of entities
            if (skip is not null)
            {
                entities = entities.Skip((int)skip);
            }
            // If user asked to only take a number of entities
            if (top is not null)
            {
                entities = entities.Take((int)top);
            }

            // Output entities as hashtables
            // We cannot output the result as TableEntity objects, since we dont (want to) expose the SDK assembly to the user session
            return entities.ToEnumerable().Select(e =>
            {
                PSObject entityObject = new();
                entityObject.Properties.Add(new PSNoteProperty("ETag", e["odata.etag"]));
                foreach (var key in e.Keys)
                {
                    if (key == "odata.etag") continue;
                    entityObject.Properties.Add(new PSNoteProperty(key, e[key]));
                }
                return entityObject;
            });
        }
        catch (Exception ex)
        {
            throw new AzDataTableException(new ErrorRecord(ex, "GetEntitiesError", ErrorCategory.InvalidOperation, null));
        }
    }

    /// <summary>
    /// Clear all entities from a table.
    /// </summary>
    public void ClearTable()
    {
        try
        {
            var entities = TableClient!.Query<TableEntity>((string)null!, null, new[] { "PartitionKey", "RowKey" });

            var transactions = new List<TableTransactionAction>();

            transactions.AddRange(entities.Select(e => new TableTransactionAction(TableTransactionActionType.Delete, e)));

            SubmitTransaction(transactions);
        }
        catch (Exception ex)
        {
            throw new AzDataTableException(new ErrorRecord(ex, "ClearTableError", ErrorCategory.InvalidOperation, null));
        }
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
                var response = TableClient!.SubmitTransaction(group.Skip(i).Take(100), CancellationToken);
                foreach (var transactionResult in response.Value)
                {
                    Console.WriteLine(transactionResult.Content.ToString());
                }
            }
        }
    }
}
