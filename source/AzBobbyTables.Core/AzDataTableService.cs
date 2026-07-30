using Azure;
using Azure.Data.Tables;
using PipeHow.AzBobbyTables.Core.Conversion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using Azure.Core;
using Azure.Core.Pipeline;
using System.Net.Http;

namespace PipeHow.AzBobbyTables.Core;

/// <summary>
/// An internal representation of the TableTransactionActionType enum.
/// </summary>
public enum OperationTypeEnum
{
    Add,
    Delete,
    UpdateMerge,
    UpdateReplace,
    UpsertMerge,
    UpsertReplace
}

public class AzDataTableService
{
    private static readonly object _lock = new object();
    private static int _maxConnectionsPerServer = 0;
    private static HttpClient _sharedHttpClient;
    private static HttpClientTransport _sharedTransport;

    /// <summary>
    /// Shared HttpClient used by all TableClient/TableServiceClient instances.
    /// Connection pooling is handled by the runtime's SocketsHttpHandler (.NET Core 3.1+).
    /// This eliminates per-cmdlet TCP connection creation.
    /// </summary>
    private static HttpClient SharedHttpClient
    {
        get
        {
            if (_sharedHttpClient == null)
            {
                lock (_lock)
                {
                    if (_sharedHttpClient == null)
                    {
                        var handler = new HttpClientHandler();
                        if (_maxConnectionsPerServer > 0)
                        {
                            handler.MaxConnectionsPerServer = _maxConnectionsPerServer;
                        }
                        _sharedHttpClient = new HttpClient(handler);
                        _sharedTransport = new HttpClientTransport(_sharedHttpClient);
                    }
                }
            }
            return _sharedHttpClient;
        }
    }

    private static HttpClientTransport SharedTransport
    {
        get
        {
            _ = SharedHttpClient;
            return _sharedTransport;
        }
    }

    /// <summary>
    /// Configures the maximum number of connections per server for the shared HTTP client.
    /// Must be called before any table operations. First call wins; subsequent calls are
    /// ignored once the HttpClient has been created.
    /// </summary>
    /// <param name="maxConnections">Maximum concurrent connections per server endpoint.</param>
    public static void ConfigureMaxConnectionsPerServer(int maxConnections)
    {
        if (_sharedHttpClient != null) return;
        lock (_lock)
        {
            if (_sharedHttpClient != null) return;
            _maxConnectionsPerServer = maxConnections;
        }
    }

    private static TableClientOptions CreateSharedOptions(int maxRetries)
    {
        var options = new TableClientOptions();
        options.Transport = SharedTransport;

        if (maxRetries > 0)
        {
            options.Retry.Mode = RetryMode.Fixed;
            options.Retry.Delay = TimeSpan.FromSeconds(1);
            options.Retry.MaxDelay = TimeSpan.FromSeconds(10);
            options.Retry.MaxRetries = maxRetries;
        }
        else
        {
            options.Retry.MaxRetries = 0;
        }

        return options;
    }

    /// <summary>
    /// The maximum number of entities Azure Table Storage accepts in a single transaction.
    /// </summary>
    private const int MaxTransactionSize = 100;

    /// <summary>
    /// The maximum page size Azure Table Storage accepts. A larger value is rejected with
    /// 400 InvalidInput rather than being capped, so any page size hint must be clamped to it.
    /// </summary>
    public const int MaxEntitiesPerPage = 1000;

    /// <summary>
    /// Calculates the page size to request for a query, or null to accept the service default.
    /// </summary>
    /// <remarks>
    /// Only a hint: it sizes each page, it does not limit the total. The caller still applies
    /// Take, and the SDK pages through transparently when more than one page is needed.
    /// Returns null when sorting, because ordering has to see every entity.
    /// The result is clamped to <see cref="MaxEntitiesPerPage"/> because the service rejects a
    /// larger page size outright with 400 InvalidInput instead of capping it. Note that Azurite
    /// does not enforce that limit, so only a direct assertion on this value catches a regression.
    /// </remarks>
    /// <param name="top">Maximum number of entities the caller asked for.</param>
    /// <param name="skip">Number of entities to skip; applied client side, so the page must cover them.</param>
    /// <param name="orderBy">Properties to sort by, if any.</param>
    /// <returns>The page size to request, or null for the service default.</returns>
    public static int? CalculatePageSize(int? top, int? skip, string[] orderBy)
    {
        if (!(top > 0) || (orderBy is not null && orderBy.Length > 0))
        {
            return null;
        }

        var skipped = skip > 0 ? skip.Value : 0;
        var needed = (long)skipped + top.Value;

        return (int)Math.Min(needed, MaxEntitiesPerPage);
    }

    /// <summary>
    /// Creates the transaction list, presized when the source count is known to avoid regrowing it.
    /// </summary>
    private static List<TableTransactionAction> CreateTransactionList(IEnumerable<object> entities) =>
        entities is ICollection<object> collection
            ? new List<TableTransactionAction>(collection.Count)
            : new List<TableTransactionAction>();

    private TableClient? TableClient { get; set; }
    private TableServiceClient? TableServiceClient { get; set; }

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

    private TableTransactionActionType ConvertOperationType(OperationTypeEnum operationType) =>
        Enum.TryParse(operationType.ToString(), out TableTransactionActionType transactionType)
            ? transactionType
            : throw new ArgumentException($"Invalid operation type: {operationType}");

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

    public static AzDataTableService CreateWithConnectionString(string connectionString, string tableName, bool createIfNotExists, CancellationToken cancellationToken, int maxRetries = 0)
    {
        try
        {
            var dataTableService = new AzDataTableService(cancellationToken);

            TableServiceClient serviceClient = new(connectionString, CreateSharedOptions(maxRetries));

            if (tableName is not null)
            {
                TableClient client = new(connectionString, tableName, CreateSharedOptions(maxRetries));

                if (createIfNotExists && !string.IsNullOrWhiteSpace(tableName))
                {
                    CreateIfNotExists(client, cancellationToken);
                }

                dataTableService.TableClient = client;
            }

            dataTableService.TableServiceClient = serviceClient;
            return dataTableService;
        }
        catch (Exception ex)
        {
            throw new AzDataTableException(new ErrorRecord(ex, "ConnectWithConnectionStringError", ErrorCategory.ConnectionError, null));
        }
    }

    public static AzDataTableService CreateWithStorageKey(string storageAccountName, string tableName, string storageAccountKey, bool createIfNotExists, CancellationToken cancellationToken, int maxRetries = 0)
    {
        try
        {
            var dataTableService = new AzDataTableService(cancellationToken);
            var tableEndpoint = new Uri($"https://{storageAccountName}.table.core.windows.net/{tableName}");

            var sasCredential = new TableSharedKeyCredential(storageAccountName, storageAccountKey);

            TableServiceClient serviceClient = new(tableEndpoint, sasCredential, CreateSharedOptions(maxRetries));

            if (tableName is not null)
            {
                TableClient client = new(tableEndpoint, tableName, sasCredential, CreateSharedOptions(maxRetries));

                if (createIfNotExists && !string.IsNullOrWhiteSpace(tableName))
                {
                    CreateIfNotExists(client, cancellationToken);
                }

                dataTableService.TableClient = client;
            }

            dataTableService.TableServiceClient = serviceClient;
            return dataTableService;
        }
        catch (Exception ex)
        {
            throw new AzDataTableException(new ErrorRecord(ex, "ConnectWithStorageKeyError", ErrorCategory.ConnectionError, null));
        }
    }

    public static AzDataTableService CreateWithToken(string storageAccountName, string tableName, string token, bool createIfNotExists, CancellationToken cancellationToken, int maxRetries = 0)
    {
        try
        {
            var dataTableService = new AzDataTableService(cancellationToken);
            var tableEndpoint = new Uri($"https://{storageAccountName}.table.core.windows.net/{tableName}");

            TableServiceClient serviceClient = new(tableEndpoint, new ExternalTokenCredential(token, DateTimeOffset.Now.Add(TimeSpan.FromHours(1))), CreateSharedOptions(maxRetries));

            if (tableName is not null)
            {
                TableClient client = new(tableEndpoint, tableName, new ExternalTokenCredential(token, DateTimeOffset.Now.Add(TimeSpan.FromHours(1))), CreateSharedOptions(maxRetries));

                if (createIfNotExists && !string.IsNullOrWhiteSpace(tableName))
                {
                    CreateIfNotExists(client, cancellationToken);
                }

                dataTableService.TableClient = client;
            }
            dataTableService.TableServiceClient = serviceClient;
            return dataTableService;
        }
        catch (Exception ex)
        {
            throw new AzDataTableException(new ErrorRecord(ex, "ConnectWithTokenError", ErrorCategory.ConnectionError, null));
        }
    }

    public static AzDataTableService CreateWithSAS(Uri sasUrl, string tableName, bool createIfNotExists, CancellationToken cancellationToken, int maxRetries = 0)
    {
        try
        {
            var dataTableService = new AzDataTableService(cancellationToken);
            // The credential is built only using the token
            var sasCredential = new AzureSasCredential(sasUrl.Query);

            // Extract the base URL (without the table name)
            var baseUrl = new Uri(sasUrl.GetLeftPart(UriPartial.Authority));

            // If the user did not specify a full endpoint to the table
            if (!sasUrl.ToString().Contains($"/{tableName}?"))
            {
                // Insert the table name before the URL parameters
                var urlParts = sasUrl.ToString().Split('?');
                sasUrl = new Uri($"{urlParts.First().TrimEnd('/')}/{tableName}?{urlParts.Last()}");
            }

            TableServiceClient serviceClient = new TableServiceClient(baseUrl, sasCredential, CreateSharedOptions(maxRetries));
            if (tableName is not null)
            {
                TableClient client = new(sasUrl, sasCredential, CreateSharedOptions(maxRetries));

                if (createIfNotExists && !string.IsNullOrWhiteSpace(tableName))
                {
                    CreateIfNotExists(client, cancellationToken);
                }

                dataTableService.TableClient = client;
            }
            dataTableService.TableServiceClient = serviceClient;
            return dataTableService;
        }
        catch (Exception ex)
        {
            throw new AzDataTableException(new ErrorRecord(ex, "ConnectWithSASError", ErrorCategory.ConnectionError, null));
        }
    }

    /// <summary>
    /// Get a list of tables from the storage account.
    /// </summary>
    /// <param name="filter">The filter string to use in the query.</param>
    /// <returns>The list of tables as strings.</returns>
    public IEnumerable<string> GetTables(string filter)
    {
        try
        {
            var tables = TableServiceClient!.Query(filter, null, CancellationToken);
            // Return Name property of each table
            return tables.Select(t => t.Name);
        }
        catch (Exception ex)
        {
            throw new AzDataTableException(new ErrorRecord(ex, "GetTablesError", ErrorCategory.InvalidOperation, null));
        }
    }

    /// <summary>
    /// Remove a table from the storage account.
    /// </summary>
    public void RemoveTable()
    {
        ValidateTableClient();

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
    /// Add one or more entities to a table using the dynamic converter system.
    /// </summary>
    /// <param name="entities">The entities to add (can be any supported type).</param>
    /// <param name="operationType">The type of operation to perform.</param>
    public void AddEntitiesToTable(IEnumerable<object> entities, OperationTypeEnum operationType)
    {
        ValidateTableClient();

        try
        {
            var transactions = CreateTransactionList(entities);
            var registry = EntityConverterRegistry.Instance;

            var tableEntities = entities.Select(entity =>
            {
                var converter = registry.GetConverter(entity);
                if (converter == null)
                {
                    var supportedTypes = string.Join(", ", registry.GetSupportedTypeNames());
                    throw new ArgumentException($"Unsupported entity type '{entity.GetType().FullName}'. Supported types are: {supportedTypes}");
                }

                // Convert first, then check the keys on the result. ValidateEntity walked every
                // key of the input looking for the two required ones, so each entity was
                // traversed twice. The converted entity is dictionary backed, and ContainsKey
                // matches ordinally, so this is the same case-sensitive presence check for two
                // lookups instead of a second full pass.
                var tableEntity = converter.ConvertToTableEntity(entity);

                if (!tableEntity.ContainsKey("PartitionKey") || !tableEntity.ContainsKey("RowKey"))
                {
                    throw new ArgumentException($"Entity of type {converter.TypeName} is missing required PartitionKey or RowKey properties!");
                }

                return tableEntity;
            });

            var transactionType = ConvertOperationType(operationType);
            transactions.AddRange(tableEntities.Select(e => new TableTransactionAction(transactionType, e)));

            SubmitTransaction(transactions);
        }
        catch (Exception ex)
        {
            throw new AzDataTableException(new ErrorRecord(ex, "AddEntitiesError", ErrorCategory.InvalidOperation, null));
        }
    }

    /// <summary>
    /// Remove one or more entities from a table using the dynamic converter system.
    /// </summary>
    /// <param name="entities">The entities to remove (can be any supported type).</param>
    /// <param name="validateEtag">Whether or not to validate that the ETag is the same and the item has not changed.</param>
    public void RemoveEntitiesFromTable(IEnumerable<object> entities, bool validateEtag = true)
    {
        ValidateTableClient();

        try
        {
            var transactions = CreateTransactionList(entities);
            var registry = EntityConverterRegistry.Instance;

            var tableEntities = entities.Select(entity =>
            {
                var converter = registry.GetConverter(entity);
                if (converter == null)
                {
                    var supportedTypes = string.Join(", ", registry.GetSupportedTypeNames());
                    throw new ArgumentException($"Unsupported entity type '{entity.GetType().FullName}'. Supported types are: {supportedTypes}");
                }

                // See AddEntitiesToTable: convert once, then check the two required keys on the
                // dictionary-backed result rather than walking the input a second time.
                var tableEntity = converter.ConvertToTableEntity(entity);

                if (!tableEntity.ContainsKey("PartitionKey") || !tableEntity.ContainsKey("RowKey"))
                {
                    throw new ArgumentException($"Entity of type {converter.TypeName} is missing required PartitionKey or RowKey properties!");
                }

                return tableEntity;
            });

            transactions.AddRange(tableEntities.Select(e => new TableTransactionAction(TableTransactionActionType.Delete, e, validateEtag ? e.ETag : default)));

            SubmitTransaction(transactions);
        }
        catch (Exception ex)
        {
            throw new AzDataTableException(new ErrorRecord(ex, "RemoveEntitiesError", ErrorCategory.InvalidOperation, null));
        }
    }

    /// <summary>
    /// Update one or more entities in a table using the dynamic converter system.
    /// </summary>
    /// <param name="entities">The entities to update (can be any supported type).</param>
    /// <param name="operationType">The type of operation to perform.</param>
    /// <param name="validateEtag">Whether or not to validate that the ETag is the same and the item has not changed.</param>
    public void UpdateEntitiesInTable(IEnumerable<object> entities, OperationTypeEnum operationType, bool validateEtag = true)
    {
        ValidateTableClient();

        try
        {
            var transactions = CreateTransactionList(entities);
            var registry = EntityConverterRegistry.Instance;

            var tableEntities = entities.Select(entity =>
            {
                var converter = registry.GetConverter(entity);
                if (converter == null)
                {
                    var supportedTypes = string.Join(", ", registry.GetSupportedTypeNames());
                    throw new ArgumentException($"Unsupported entity type '{entity.GetType().FullName}'. Supported types are: {supportedTypes}");
                }

                // See AddEntitiesToTable: convert once, then check the two required keys on the
                // dictionary-backed result rather than walking the input a second time.
                var tableEntity = converter.ConvertToTableEntity(entity);

                if (!tableEntity.ContainsKey("PartitionKey") || !tableEntity.ContainsKey("RowKey"))
                {
                    throw new ArgumentException($"Entity of type {converter.TypeName} is missing required PartitionKey or RowKey properties!");
                }

                return tableEntity;
            });

            var transactionType = ConvertOperationType(operationType);
            transactions.AddRange(tableEntities.Select(e => new TableTransactionAction(transactionType, e, validateEtag ? e.ETag : default)));

            SubmitTransaction(transactions);
        }
        catch (Exception ex)
        {
            throw new AzDataTableException(new ErrorRecord(ex, "UpdateEntitiesError", ErrorCategory.InvalidOperation, null));
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
        ValidateTableClient();

        try
        {
            // When the caller wants a bounded number of entities and no sorting is needed, ask the
            // service for only that many per page. Otherwise the first page returns up to 1000
            // entities and everything past the requested count is fetched and then discarded.
            var maxPerPage = CalculatePageSize(top, skip, orderBy);

            // Declare type as IEnumerable to be able to overwrite it with LINQ results further down.
            // Query rather than QueryAsync: the result is handed back to PowerShell synchronously either way
            IEnumerable<TableEntity> entities = TableClient!.Query<TableEntity>(query, maxPerPage, properties, CancellationToken);

            // If user specified one or more properties to sort list by
            // This may slow the query down a lot with a lot of results
            if (orderBy is not null && orderBy.Any())
            {
                // Must create a new variable to be able to modify it within the loop
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
            return entities.Select(e =>
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
    /// Count the entities in a table matching a query.
    /// </summary>
    /// <param name="query">The query to filter entities by.</param>
    /// <returns>The number of matching entities.</returns>
    public int CountEntitiesInTable(string query)
    {
        ValidateTableClient();

        try
        {
            // Only the keys are requested, and the entities are counted without being projected
            // into PSObjects, since the caller only wants the total.
            var entities = TableClient!.Query<TableEntity>(query, null, ["PartitionKey", "RowKey"], CancellationToken);

            var count = 0;
            foreach (var _ in entities)
            {
                count++;
            }

            return count;
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
        ValidateTableClient();

        try
        {
            var entities = TableClient!.Query<TableEntity>((string)null!, null, ["PartitionKey", "RowKey"]);

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
        ValidateTableClient();

        // Transactions only support up to 100 entities of the same partitionkey
        // Loop through transactions grouped by partitionkey
        foreach (var group in transactions.GroupBy(t => t.Entity.PartitionKey))
        {
            // Loop through each group and submit up to 100 at a time
            var count = group.Count();
            for (int i = 0; i < count; i += MaxTransactionSize)
            {
                TableClient!.SubmitTransaction(group.Skip(i).Take(MaxTransactionSize), CancellationToken);
            }
        }
    }

    /// <summary>
    /// Validate that the TableName is set in the context.
    /// </summary>
    private void ValidateTableClient()
    {
        if (TableClient is null)
        {
            throw new AzDataTableException(new ErrorRecord(new InvalidOperationException("Table name is not set in the TableContext, please create a new context for this operation!"), "TableClientError", ErrorCategory.InvalidOperation, null));
        }
    }

    /// <summary>
    /// Gets the list of supported entity type names.
    /// </summary>
    /// <returns>A list of supported type names.</returns>
    public static IEnumerable<string> GetSupportedEntityTypes()
    {
        return EntityConverterRegistry.Instance.GetSupportedTypeNames();
    }

    #region Large entity operations

    /// <summary>
    /// Properties to request when only the row identity is needed.
    /// </summary>
    private static readonly string[] KeysOnlyProperties = { "PartitionKey", "RowKey" };

    /// <summary>
    /// Number of RowKeys to combine into one OData filter when looking up part rows.
    /// </summary>
    private const int PartLookupChunkSize = 10;

    /// <summary>
    /// Add one or more entities to a table, transparently splitting entities that
    /// exceed the Azure Table Storage size limits across multiple properties and rows.
    /// See <see cref="EntitySplitter"/> for the storage format.
    ///
    /// Entities are deduplicated by PartitionKey and RowKey (the last occurrence wins)
    /// so one call can safely carry several versions of the same logical entity, and
    /// leftover part rows from earlier, larger versions of a split entity are removed
    /// after a successful write.
    /// </summary>
    /// <param name="entities">The entities to add (can be any supported type).</param>
    /// <param name="operationType">The type of operation to perform.</param>
    public void AddLargeEntitiesToTable(IEnumerable<object> entities, OperationTypeEnum operationType)
    {
        ValidateTableClient();

        try
        {
            var transactionType = ConvertOperationType(operationType);

            // Deduplicate by entity identity, last one wins. A transaction containing
            // two operations for the same key is rejected by the service outright.
            var order = new List<(string PartitionKey, string RowKey)>();
            var latest = new Dictionary<(string PartitionKey, string RowKey), TableEntity>();
            foreach (var entity in entities)
            {
                var tableEntity = ConvertToValidatedTableEntity(entity);
                var key = (tableEntity.PartitionKey, tableEntity.RowKey);
                if (!latest.ContainsKey(key))
                {
                    order.Add(key);
                }
                latest[key] = tableEntity;
            }

            var actions = new List<TableTransactionAction>(order.Count);
            var splitEntities = new List<(string PartitionKey, string RowKey, HashSet<string> LiveRowKeys)>();

            foreach (var key in order)
            {
                var result = EntitySplitter.Split(latest[key]);

                if (!result.Engaged)
                {
                    actions.Add(new TableTransactionAction(transactionType, result.Rows[0]));
                    continue;
                }

                // Merging cannot remove properties from existing rows, and the
                // property distribution over rows is not stable between writes, so a
                // merged multi-row write would leave stale values behind on other rows
                // and corrupt the reassembled entity. Multi-row merges are therefore
                // promoted to full replacement.
                var rowActionType = result.Rows.Count > 1 && transactionType == TableTransactionActionType.UpsertMerge
                    ? TableTransactionActionType.UpsertReplace
                    : transactionType;

                foreach (var row in result.Rows)
                {
                    actions.Add(new TableTransactionAction(rowActionType, row));
                }

                splitEntities.Add((key.PartitionKey, key.RowKey, new HashSet<string>(result.Rows.Select(r => r.RowKey), StringComparer.Ordinal)));
            }

            SubmitTransactionSized(actions);

            // Remove leftover part rows from earlier writes that used more rows than
            // this one, so stale data cannot leak into reassembly.
            foreach (var (partitionKey, rowKey, liveRowKeys) in splitEntities)
            {
                RemoveStalePartRows(partitionKey, rowKey, liveRowKeys);
            }
        }
        catch (Exception ex)
        {
            throw new AzDataTableException(new ErrorRecord(ex, "AddLargeEntitiesError", ErrorCategory.InvalidOperation, null));
        }
    }

    /// <summary>
    /// Get entities from a table based on an OData query, reassembling entities that
    /// were split across multiple properties or rows by the large-entity write path.
    /// See <see cref="EntitySplitter"/> for the storage format.
    ///
    /// Sorting, skipping and taking apply to the physical rows before reassembly, and
    /// property selection that excludes the split markers prevents reassembly, so
    /// filters should target the PartitionKey level when split entities are involved.
    /// </summary>
    /// <param name="query">The query to filter entities by.</param>
    /// <param name="properties">The list of properties to return.</param>
    /// <param name="top">The maximum number of physical rows to retrieve.</param>
    /// <param name="skip">The number of physical rows to skip.</param>
    /// <param name="orderBy">The names of one or more properties to sort by.</param>
    /// <param name="onWarning">Called with a message when a malformed split manifest is skipped.</param>
    /// <returns>The reassembled entities.</returns>
    public IEnumerable<PSObject> GetLargeEntitiesFromTable(string query, string[] properties = null!, int? top = null, int? skip = null, string[] orderBy = null!, Action<string> onWarning = null!)
    {
        ValidateTableClient();

        try
        {
            // Page size hint, same reasoning as GetEntitiesFromTable.
            var maxPerPage = CalculatePageSize(top, skip, orderBy);

            IEnumerable<TableEntity> entities = TableClient!.Query<TableEntity>(query, maxPerPage, properties, CancellationToken);

            if (orderBy is not null && orderBy.Any())
            {
                var orderableEntities = entities.OrderBy(e => e[orderBy.First()]);
                foreach (var propertyName in orderBy.Skip(1))
                {
                    orderableEntities = orderableEntities.ThenBy(e => e[propertyName]);
                }
                entities = orderableEntities;
            }
            if (skip is not null)
            {
                entities = entities.Skip((int)skip);
            }
            if (top is not null)
            {
                entities = entities.Take((int)top);
            }

            // Reassembly needs the whole result set: part rows carry no ordering
            // guarantee relative to their siblings once sorting or filters apply.
            return EntitySplitter.Reassemble(entities.ToList(), onWarning)
                .Select(ProjectToPSObject)
                .ToList();
        }
        catch (Exception ex)
        {
            throw new AzDataTableException(new ErrorRecord(ex, "GetLargeEntitiesError", ErrorCategory.InvalidOperation, null));
        }
    }

    /// <summary>
    /// Remove one or more entities from a table, including any part rows the entities
    /// were split into by the large-entity write path. Part rows are looked up by the
    /// OriginalEntityId marker and removed unconditionally; ETag validation, when
    /// requested, applies to the entity's own row.
    /// </summary>
    /// <param name="entities">The entities to remove (can be any supported type).</param>
    /// <param name="validateEtag">Whether or not to validate that the ETag is the same and the item has not changed.</param>
    public void RemoveLargeEntitiesFromTable(IEnumerable<object> entities, bool validateEtag = true)
    {
        ValidateTableClient();

        try
        {
            var mainRows = entities.Select(ConvertToValidatedTableEntity).ToList();
            var mainKeys = new HashSet<(string PartitionKey, string RowKey)>(mainRows.Select(e => (e.PartitionKey, e.RowKey)));

            var actions = new List<TableTransactionAction>(mainRows.Count);
            foreach (var entity in mainRows)
            {
                actions.Add(new TableTransactionAction(TableTransactionActionType.Delete, entity, validateEtag ? entity.ETag : default));
            }

            // Find the part rows belonging to the removed entities. OriginalEntityId
            // is matched in chunked OR filters to keep the number of queries low.
            foreach (var partitionGroup in mainRows.GroupBy(e => e.PartitionKey))
            {
                var rowKeys = partitionGroup.Select(e => e.RowKey).Distinct(StringComparer.Ordinal).ToList();
                for (var i = 0; i < rowKeys.Count; i += PartLookupChunkSize)
                {
                    var conditions = string.Join(" or ", rowKeys
                        .Skip(i)
                        .Take(PartLookupChunkSize)
                        .Select(rowKey => $"{EntitySplitter.OriginalEntityIdKey} eq '{EscapeODataValue(rowKey)}'"));
                    var filter = $"PartitionKey eq '{EscapeODataValue(partitionGroup.Key)}' and ({conditions})";

                    foreach (var partRow in TableClient!.Query<TableEntity>(filter, null, KeysOnlyProperties, CancellationToken))
                    {
                        if (!mainKeys.Contains((partRow.PartitionKey, partRow.RowKey)))
                        {
                            actions.Add(new TableTransactionAction(TableTransactionActionType.Delete, partRow));
                        }
                    }
                }
            }

            SubmitTransactionSized(actions);
        }
        catch (Exception ex)
        {
            throw new AzDataTableException(new ErrorRecord(ex, "RemoveLargeEntitiesError", ErrorCategory.InvalidOperation, null));
        }
    }

    /// <summary>
    /// Convert an input entity through the converter registry and validate that it
    /// carries the required keys.
    /// </summary>
    private static TableEntity ConvertToValidatedTableEntity(object entity)
    {
        var registry = EntityConverterRegistry.Instance;
        var converter = registry.GetConverter(entity);
        if (converter == null)
        {
            var supportedTypes = string.Join(", ", registry.GetSupportedTypeNames());
            throw new ArgumentException($"Unsupported entity type '{entity.GetType().FullName}'. Supported types are: {supportedTypes}");
        }

        var tableEntity = converter.ConvertToTableEntity(entity);

        if (!tableEntity.ContainsKey("PartitionKey") || !tableEntity.ContainsKey("RowKey"))
        {
            throw new ArgumentException($"Entity of type {converter.TypeName} is missing required PartitionKey or RowKey properties!");
        }

        // Unwrap PSObject-wrapped values, which PowerShell pipelines produce readily
        // (e.g. @{ N = $_ } inside ForEach-Object) and the SDK serializer rejects.
        foreach (var key in tableEntity.Keys.ToList())
        {
            if (tableEntity[key] is PSObject wrapped)
            {
                tableEntity[key] = wrapped.BaseObject;
            }
        }

        return tableEntity;
    }

    /// <summary>
    /// Project a table entity to the PSObject shape returned to the user session.
    /// </summary>
    private static PSObject ProjectToPSObject(TableEntity entity)
    {
        PSObject entityObject = new();
        entityObject.Properties.Add(new PSNoteProperty("ETag", entity["odata.etag"]));
        foreach (var key in entity.Keys)
        {
            if (key == "odata.etag") continue;
            entityObject.Properties.Add(new PSNoteProperty(key, entity[key]));
        }
        return entityObject;
    }

    /// <summary>
    /// Submit transactions grouped by partition, packing each batch by count and by
    /// estimated payload size. Rows produced by entity splitting approach the row
    /// budget individually, so packing 100 of them into one transaction would exceed
    /// the service's batch payload limit.
    /// </summary>
    private void SubmitTransactionSized(IList<TableTransactionAction> transactions)
    {
        foreach (var group in transactions.GroupBy(t => t.Entity.PartitionKey))
        {
            var batch = new List<TableTransactionAction>();
            long batchSize = 0;

            foreach (var action in group)
            {
                var actionSize = action.Entity is TableEntity tableEntity ? EntitySplitter.EstimateEntitySize(tableEntity) : 0;

                if (batch.Count > 0 && (batch.Count >= MaxTransactionSize || batchSize + actionSize > EntitySplitter.MaxTransactionPayload))
                {
                    FlushSizedBatch(batch);
                    batch.Clear();
                    batchSize = 0;
                }

                batch.Add(action);
                batchSize += actionSize;
            }

            if (batch.Count > 0)
            {
                FlushSizedBatch(batch);
            }
        }
    }

    /// <summary>
    /// Submit one transaction batch. When the transaction is rejected because of a
    /// single entity, fall back to executing the actions individually so the healthy
    /// entities still land; the first individual failure surfaces to the caller.
    /// </summary>
    private void FlushSizedBatch(List<TableTransactionAction> batch)
    {
        try
        {
            TableClient!.SubmitTransaction(batch, CancellationToken);
        }
        catch (TableTransactionFailedException)
        {
            foreach (var action in batch)
            {
                ExecuteTransactionActionIndividually(action);
            }
        }
    }

    /// <summary>
    /// Execute a single transaction action as its equivalent non-batched operation.
    /// </summary>
    private void ExecuteTransactionActionIndividually(TableTransactionAction action)
    {
        // All actions on this path are created from TableEntity instances; the SDK
        // serializes the concrete dictionary-backed type.
        var entity = (TableEntity)action.Entity;
        var etag = action.ETag == default ? ETag.All : action.ETag;

        switch (action.ActionType)
        {
            case TableTransactionActionType.Add:
                TableClient!.AddEntity(entity, CancellationToken);
                break;
            case TableTransactionActionType.UpsertMerge:
                TableClient!.UpsertEntity(entity, TableUpdateMode.Merge, CancellationToken);
                break;
            case TableTransactionActionType.UpsertReplace:
                TableClient!.UpsertEntity(entity, TableUpdateMode.Replace, CancellationToken);
                break;
            case TableTransactionActionType.UpdateMerge:
                TableClient!.UpdateEntity(entity, etag, TableUpdateMode.Merge, CancellationToken);
                break;
            case TableTransactionActionType.UpdateReplace:
                TableClient!.UpdateEntity(entity, etag, TableUpdateMode.Replace, CancellationToken);
                break;
            case TableTransactionActionType.Delete:
                try
                {
                    TableClient!.DeleteEntity(entity.PartitionKey, entity.RowKey, etag, CancellationToken);
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                    // The row is already gone, which is the goal of a delete.
                }
                break;
        }
    }

    /// <summary>
    /// Remove part rows of a previously split entity that are no longer used by its
    /// current version, so their stale properties cannot resurface on reassembly.
    /// </summary>
    private void RemoveStalePartRows(string partitionKey, string originalRowKey, HashSet<string> liveRowKeys)
    {
        var filter = $"PartitionKey eq '{EscapeODataValue(partitionKey)}' and {EntitySplitter.OriginalEntityIdKey} eq '{EscapeODataValue(originalRowKey)}'";

        var staleActions = TableClient!.Query<TableEntity>(filter, null, KeysOnlyProperties, CancellationToken)
            .Where(e => !liveRowKeys.Contains(e.RowKey))
            .Select(e => new TableTransactionAction(TableTransactionActionType.Delete, e))
            .ToList();

        if (staleActions.Count > 0)
        {
            SubmitTransactionSized(staleActions);
        }
    }

    /// <summary>
    /// Escape a value for use inside single quotes in an OData filter.
    /// </summary>
    private static string EscapeODataValue(string value) => value.Replace("'", "''");

    #endregion
}
