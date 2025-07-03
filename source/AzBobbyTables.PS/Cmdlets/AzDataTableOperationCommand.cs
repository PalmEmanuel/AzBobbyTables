using PipeHow.AzBobbyTables.Core;
using PipeHow.AzBobbyTables.Core.Conversion;
using System;
using System.Collections;
using System.Linq;
using System.Management.Automation;
using System.Threading;

namespace PipeHow.AzBobbyTables.Cmdlets;

public class AzDataTableOperationCommand : AzDataTableCommand
{
    protected override void BeginProcessing()
    {
        base.BeginProcessing();

        try
        {
            var parameters = MyInvocation.BoundParameters;

            // If the user specified the -Entity parameter, validate the data types of input
            if (parameters.ContainsKey("Entity"))
            {
                // Only writes a warning to the user if it doesn't match expected types
                ValidateEntitiesAndWarn();
            }

            // Mandatory
            AzDataTableContext context = (AzDataTableContext)parameters["Context"];

            // If switch was provided and true, or if command is New-AzDataTable, create table
            bool createIfNotExists = (
                    parameters.ContainsKey("CreateTableIfNotExists") &&
                    ((SwitchParameter)parameters["CreateTableIfNotExists"]).IsPresent
                ) || MyInvocation.MyCommand.Name is "New-AzDataTable";

            tableService = CreateWithContext(context, createIfNotExists, cancellationTokenSource.Token);
        }
        catch (AzDataTableException ex)
        {
            WriteError(ex.ErrorRecord);
        }
    }

    // Determine way to create AzDataTableService by using the provided Context, created with from New-AzDataTableContext
    private AzDataTableService CreateWithContext(AzDataTableContext context, bool createIfNotExists, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(context.TableName) && createIfNotExists)
        {
            throw new AzDataTableException(new ErrorRecord(
                new InvalidOperationException("The provided TableContext must have a table name specified to create it."),
                "TableNameRequiredError",
                ErrorCategory.InvalidArgument,
                null));
        }

        try
        {
            return context.ConnectionType switch
            {
                AzDataTableConnectionType.ConnectionString => AzDataTableService.CreateWithConnectionString(context.ConnectionString, context.TableName, createIfNotExists, cancellationToken),
                AzDataTableConnectionType.SAS => AzDataTableService.CreateWithSAS(context.SharedAccessSignature, context.TableName, createIfNotExists, cancellationToken),
                AzDataTableConnectionType.Key => AzDataTableService.CreateWithStorageKey(context.StorageAccountName, context.TableName, context.StorageAccountKey, createIfNotExists, cancellationToken),
                AzDataTableConnectionType.Token => AzDataTableService.CreateWithToken(context.StorageAccountName, context.TableName, context.Token, createIfNotExists, cancellationToken),
                AzDataTableConnectionType.ManagedIdentity => AzDataTableService.CreateWithToken(context.StorageAccountName, context.TableName, Helpers.GetManagedIdentityToken(context.StorageAccountName, context.ClientId), createIfNotExists, cancellationToken),
                _ => throw new ArgumentException($"Unknown connection type {context.ConnectionType} was used!"),
            };
        }
        catch (AzDataTableException ex)
        {
            throw new AzDataTableException(new ErrorRecord(
                ex.ErrorRecord.Exception,
                $"ConnectByContextWith{context.ConnectionType}Error",
                ErrorCategory.InvalidOperation,
                null));
        }
    }

    /// <summary>
    /// Validate the data types of user input to ensure it matches the supported entity and table data types.
    /// Also warn if any value is null.
    /// </summary>
    protected void ValidateEntitiesAndWarn()
    {
        var entities = (object[])MyInvocation.BoundParameters["Entity"];
        var firstEntity = entities.First();
        
        // First, validate that the entity type itself is supported
        var registry = EntityConverterRegistry.Instance;
        var converter = registry.GetConverter(firstEntity);
        
        if (converter == null)
        {
            var supportedTypes = string.Join(", ", registry.GetSupportedTypeNames());
            throw new ArgumentException($"Entity type '{firstEntity.GetType().Name}' is not supported. Supported entity types are: {supportedTypes}");
        }

        try
        {            
            // Check for null values
            if (converter.ValidateEntityPropertyValuesNotNull(firstEntity, out var nullProperties) is not true)
            {
                WriteWarning($"One of the provided entities has at least one null property value, which will not be included in the table operation: {string.Join(", ", nullProperties)}");
            }

            // Check for unsupported property value types
            if (converter.ValidateEntityPropertyTypes(firstEntity, out var unsupportedProperties) is not true)
            {
                string warningMessage = $@"An input entity has at least one property with a potentially unsupported type, please ensure that the input entities have only supported data types. Unsupported property types found:
{string.Join(", ", unsupportedProperties)}

https://docs.microsoft.com/en-us/rest/api/storageservices/understanding-the-table-service-data-model#property-types";
                WriteWarning(warningMessage);
            }
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(ex, $"Entity{firstEntity.GetType().Name}ValidationError", ErrorCategory.InvalidData, firstEntity));
        }
    }
}
