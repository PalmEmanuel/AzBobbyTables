using PipeHow.AzBobbyTables.Validation;
using System.Collections;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets;

/// <summary>
/// <para type="synopsis">Remove one or more entities from an Azure Table.</para>
/// </summary>
[Cmdlet(VerbsCommon.Remove, "AzDataTableEntity")]
public class RemoveAzDataTableEntity : AzDataTableOperationCommand
{
    /// <summary>
    /// <para type="description">The entities to remove from the table.</para>
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "TableOperation", ValueFromPipeline = true)]
    [ValidateEntity()]
    public Hashtable[] Entity { get; set; }

    /// <summary>
    /// The process step of the pipeline.
    /// </summary>
    protected override void ProcessRecord() => tableService.RemoveEntitiesFromTable(Entity);
}
