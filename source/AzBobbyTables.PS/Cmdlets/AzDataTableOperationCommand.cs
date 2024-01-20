using PipeHow.AzBobbyTables.Core;
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
    /// Validate the data types of user input to ensure it matches the supported table data types.
    /// Also warn if any value is null.
    /// </summary>
    protected void ValidateEntitiesAndWarn()
    {
        switch (((object[])MyInvocation.BoundParameters["Entity"]).First())
        {
            case Hashtable firstEntity:
                try
                {
                    // Use OfType to get an enumerable of the keys, to select all values
                    var values = firstEntity.Values.Cast<object>();

                    // ValidateEntitiesAndWarn if any null values
                    if (values.Any(v => v is null)) {
                        WriteWarning("One of the provided entities has a null property value, which will not be included to the table operation.");
                    }

                    // Write warning if any values are of unsupported datatypes
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
                    WriteError(new ErrorRecord(ex, "EntityTypeHashtableValidationError", ErrorCategory.InvalidData, firstEntity));
                }
                break;
            case PSObject firstEntity:
                try
                {
                    // Use OfType to get an enumerable of the keys, to select all values
                    var properties = firstEntity.Properties;
                    // ValidateEntitiesAndWarn if any null values
                    if (properties.Any(p => p.Value is null)) { WriteWarning("One of the provided entities has a null property value, which will not be included to the table operation."); }

                    if (properties.Any(p => p.Value is not null && !AzDataTableService.SupportedTypeList.Contains(p.Value.GetType().Name.ToLower())))
                    {
                        string warningMessage = $@"An input entity has a field with a potentially unsupported type, please ensure that the input entities have only supported data types: https://docs.microsoft.com/en-us/rest/api/storageservices/understanding-the-table-service-data-model#property-types

Example of first entity provided
--------------------------------
{string.Join("\n", firstEntity.Properties.Select(p => string.Join("\n", $"[{p.Value.GetType().FullName}] {p.Name}")))}
";
                        WriteWarning(warningMessage);
                    }
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(ex, "EntityTypePSObjectValidationError", ErrorCategory.InvalidData, firstEntity));
                }
                break;
            default:
                throw new ArgumentException("Entities provided were not Hashtable or PSObject!");
        }
    }
}
