using PipeHow.AzBobbyTables.Core;
using PipeHow.AzBobbyTables.Validation;
using System;
using System.Collections;
using System.Linq;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets;

/// <summary>
/// <para type="synopsis">Add one or more entities to an Azure Table.</para>
/// </summary>
[Cmdlet(VerbsCommon.Add, "AzDataTableEntity", DefaultParameterSetName = "OperationType")]
public class AddAzDataTableEntity : AzDataTableOperationCommand
{
    /// <summary>
    /// <para type="description">The context used for the table, created with New-AzDataTableContext.</para>
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "OperationType")]
    [Parameter(Mandatory = true, ParameterSetName = "Force")]
    public AzDataTableContext Context { get; set; }

    /// <summary>
    /// <para type="description">The entities to add to the table.</para>
    /// </summary>
    [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "OperationType")]
    [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Force")]
    [ValidateEntity]
    [ValidateNotNullOrEmpty()]
    public object[] Entity { get; set; }

    /// <summary>
    /// <para type="description">The type of operation to perform on the entities, defaults to Add.</para>
    /// </summary>
    [Parameter(ParameterSetName = "OperationType")]
    [ValidateSet("Add", "UpsertReplace", "UpsertMerge")]
    public string OperationType { get; set; } = "Add";

    /// <summary>
    /// <para type="description">Overwrites provided entities if they exist.</para>
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "Force")]
    public SwitchParameter Force { get; set; }

    /// <summary>
    /// <para type="description">If the table should be created if it does not exist.</para>
    /// </summary>
    [Parameter(ParameterSetName = "OperationType")]
    [Parameter(ParameterSetName = "Force")]
    public SwitchParameter CreateTableIfNotExists { get; set; }

    /// <summary>
    /// The process step of the pipeline.
    /// </summary>
    protected override void ProcessRecord()
    {
        if (tableService is null)
        {
            WriteError(new ErrorRecord(new InvalidOperationException("Could not establish connection!"), "ConnectionError", ErrorCategory.ConnectionError, null));
            return;
        }

        if (Force.IsPresent)
        {
            OperationType = "UpsertReplace";
        }

        var operationTypeValue = Enum.TryParse<OperationTypeEnum>(OperationType, true, out var operationType)
            ? operationType : throw new ArgumentException($"Operation type {OperationType} is not valid!");

        try
        {
            switch (Entity.First())
            {
                case Hashtable:
                    tableService.AddEntitiesToTable(Entity.Cast<Hashtable>(), operationTypeValue);
                    break;
                case PSObject:
                    tableService.AddEntitiesToTable(Entity.Cast<PSObject>(), operationTypeValue);
                    break;
                default:
                    throw new ArgumentException($"Entities provided were not Hashtable or PSObject! First entity was of type {Entity.GetType().FullName}!");
            }
        }
        catch (AzDataTableException ex)
        {
            WriteError(ex.ErrorRecord);
        }
    }
}
