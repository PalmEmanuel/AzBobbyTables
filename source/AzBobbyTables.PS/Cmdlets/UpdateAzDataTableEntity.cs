using PipeHow.AzBobbyTables.Core;
using PipeHow.AzBobbyTables.Validation;
using System;
using System.Collections;
using System.Collections.Generic;
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
    /// Entities gathered from the pipeline, submitted as one batch in EndProcessing.
    /// </summary>
    private readonly List<object> entities = new();

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

        if (!Enum.TryParse<OperationTypeEnum>(OperationType, true, out _))
        {
            WriteError(new ErrorRecord(new ArgumentException($"Operation type {OperationType} is not valid!"), "InvalidOperationType", ErrorCategory.InvalidArgument, OperationType));
            return;
        }

        // Collect rather than submit. Entity binds one pipeline record at a time, so submitting
        // here sent a separate transaction per entity instead of batching up to 100.
        entities.AddRange(Entity);
    }

    /// <summary>
    /// Submit everything gathered from the pipeline as a single batched operation.
    /// The operation type is re-parsed here rather than cached in a field: a field typed from
    /// AzBobbyTables.Core forces that assembly to load while PowerShell inspects the cmdlet type,
    /// which happens before OnImport registers the dependency load context.
    /// </summary>
    protected override void EndProcessing()
    {
        if (tableService is null || entities.Count == 0)
        {
            return;
        }

        if (!Enum.TryParse<OperationTypeEnum>(OperationType, true, out var operationTypeValue))
        {
            return;
        }

        try
        {
            tableService.UpdateEntitiesInTable(entities, operationTypeValue, !Force.IsPresent);
        }
        catch (AzDataTableException ex)
        {
            WriteError(ex.ErrorRecord);
        }
    }
}
