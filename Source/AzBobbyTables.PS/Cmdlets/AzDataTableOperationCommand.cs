using PipeHow.AzBobbyTables.Core;
using System;
using System.Collections;
using System.Linq;
using System.Management.Automation;
using System.Threading;

namespace PipeHow.AzBobbyTables.Cmdlets;

public class AzDataTableOperationCommand : AzDataTableCommand
{
    /// <summary>
    /// <para type="description">The context used for the table, created with New-AzDataTableContext.</para>
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "TableOperation", ValueFromPipelineByPropertyName = true, Position = 0)]
    [Parameter(Mandatory = true, ParameterSetName = "Count", ValueFromPipelineByPropertyName = true, Position = 0)]
    public AzDataTableContext Context { get; set; }

    /// <summary>
    /// <para type="description">If the table should be created if it does not exist.</para>
    /// </summary>
    [Parameter(ParameterSetName = "TableOperation")]
    [Parameter(ParameterSetName = "Count")]
    public SwitchParameter CreateTableIfNotExists { get; set; }

    protected override void BeginProcessing()
    {
        base.BeginProcessing();

        // If the user specified the -Entity parameter, validate the data types of input
        if (MyInvocation.BoundParameters.ContainsKey("Entity"))
        {
            // Only writes a warning to the user if it doesn't
            ValidateEntitiesAndWarn();
        }

        tableService = CreateWithContext(Context, CreateTableIfNotExists, cancellationTokenSource.Token);
    }

    // Determine way to create AzDataTableService by using the provided Context, created with from New-AzDataTableContext
    private AzDataTableService CreateWithContext(AzDataTableContext context, bool createIfNotExists, CancellationToken cancellationToken = default)
    {
        return context.ConnectionType switch
        {
            AzDataTableConnectionType.ConnectionString => AzDataTableService.CreateWithConnectionString(context.ConnectionString, context.TableName, createIfNotExists, cancellationToken),
            AzDataTableConnectionType.SAS => AzDataTableService.CreateWithSAS(context.SharedAccessSignature, context.TableName, createIfNotExists, cancellationToken),
            AzDataTableConnectionType.Key => AzDataTableService.CreateWithStorageKey(context.StorageAccountName, context.TableName, context.StorageAccountKey, createIfNotExists, cancellationToken),
            AzDataTableConnectionType.Token => AzDataTableService.CreateWithToken(context.StorageAccountName, context.TableName, context.Token, createIfNotExists, cancellationToken),
            AzDataTableConnectionType.ManagedIdentity => AzDataTableService.CreateWithToken(context.StorageAccountName, context.TableName, Helpers.GetManagedIdentityToken(context.StorageAccountName), createIfNotExists, cancellationToken),
            _ => throw new ArgumentException($"Unknown connection type {context.ConnectionType} was used!"),
        };
    }

    /// <summary>
    /// Validate the data types of user input to ensure it matches the supported table data types.
    /// Also warn if any value is null.
    /// </summary>
    protected void ValidateEntitiesAndWarn()
    {
        Hashtable[] entities = MyInvocation.BoundParameters["Entity"] as Hashtable[];

        try
        {
            // Use OfType to get an enumerable of the keys, to select all values
            var values = entities.SelectMany(h => h.Keys.OfType<string>().Select(k => h[k]));
            // ValidateEntitiesAndWarn if any null values
            if (values.Any(v => v is null)) { WriteWarning("One of the provided entities has a null property value, which will not be included to the table operation."); }

            var firstEntity = entities.First();
            if (values.Any(v => v is not null && !AzDataTableService.SupportedTypeList.Contains(v.GetType().Name.ToLower())))
            {
                string warningMessage = $@"An input entity has a field with a potentially unsupported type, please ensure that the input entities have only supported data types: https://docs.microsoft.com/en-us/rest/api/storageservices/understanding-the-table-service-data-model#property-types

Example of first entity provided
--------------------------------
{string.Join("\n", firstEntity.Keys.OfType<string>().Select(k => string.Join("\n", $"[{firstEntity[k].GetType().FullName}] {k}")))}
";
                WriteWarning(warningMessage);
            }
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(ex, "EntityTypeValidationFailed", ErrorCategory.InvalidData, entities));
        }
    }
}
