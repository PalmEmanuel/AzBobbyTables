using PipeHow.AzBobbyTables.Validation;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets;

/// <summary>
/// <para type="synopsis">Remove one or more entities from an Azure Table, including any part rows the entities were split into by Add-AzDataTableLargeEntity.</para>
/// </summary>
[Cmdlet(VerbsCommon.Remove, "AzDataTableLargeEntity")]
public class RemoveAzDataTableLargeEntity : AzDataTableOperationCommand
{
    /// <summary>
    /// <para type="description">The context used for the table, created with New-AzDataTableContext.</para>
    /// </summary>
    [Parameter(Mandatory = true)]
    public AzDataTableContext Context { get; set; }

    /// <summary>
    /// <para type="description">The entities to remove from the table.</para>
    /// </summary>
    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    [ValidateNotNullOrEmpty()]
    [ValidateEntity()]
    public object[] Entity { get; set; }

    /// <summary>
    /// <para type="description">Skips ETag validation and removes entity even if it has changed.</para>
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

        // Collect rather than submit, so that entities from the pipeline are batched
        // into as few transactions as possible when the pipeline completes.
        entities.AddRange(Entity);
    }

    /// <summary>
    /// Submit everything gathered from the pipeline as a single batched operation.
    /// </summary>
    protected override void EndProcessing()
    {
        if (tableService is null || entities.Count == 0)
        {
            return;
        }

        try
        {
            tableService.RemoveLargeEntitiesFromTable(entities, !Force.IsPresent);
        }
        catch (AzDataTableException ex)
        {
            WriteError(ex.ErrorRecord);
        }
    }
}
