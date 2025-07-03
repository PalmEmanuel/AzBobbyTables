using PipeHow.AzBobbyTables.Core;
using PipeHow.AzBobbyTables.Validation;
using System;
using System.Collections;
using System.Linq;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets;

/// <summary>
/// <para type="synopsis">Update one or more entities in an Azure Table.</para>
/// </summary>
[Cmdlet(VerbsData.Update, "AzDataTableEntity")]
public class UpdateAzDataTableEntity : AzDataTableOperationCommand
{
    /// <summary>
    /// <para type="description">The context used for the table, created with New-AzDataTableContext.</para>
    /// </summary>
    [Parameter(Mandatory = true)]
    public AzDataTableContext Context { get; set; }

    /// <summary>
    /// <para type="description">The entities to update in the table.</para>
    /// </summary>
    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    [ValidateNotNullOrEmpty()]
    [ValidateEntity()]
    public object[] Entity { get; set; }

    /// <summary>
    /// <para type="description">The type of operation to perform on the entities, defaults to Add.</para>
    /// </summary>
    [Parameter()]
    [ValidateSet("UpdateMerge", "UpdateReplace")]
    public string OperationType { get; set; } = "UpdateMerge";

    /// <summary>
    /// <para type="description">Skips ETag validation and updates entity even if it has changed.</para>
    /// </summary>
    [Parameter()]
    public SwitchParameter Force { get; set; }

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

        var operationTypeValue = Enum.TryParse<OperationTypeEnum>(OperationType, true, out var operationType)
            ? operationType : throw new ArgumentException($"Operation type {OperationType} is not valid!");

        try
        {
            tableService.UpdateEntitiesInTable(Entity, operationTypeValue, !Force.IsPresent);
        }
        catch (AzDataTableException ex)
        {
            WriteError(ex.ErrorRecord);
        }
    }
}
