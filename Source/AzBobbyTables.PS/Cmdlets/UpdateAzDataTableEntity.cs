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
    /// The process step of the pipeline.
    /// </summary>
    protected override void ProcessRecord()
    {
        switch (Entity.First())
        {
            case Hashtable:
                tableService.UpdateEntitiesInTable(Entity.Cast<Hashtable>());
                break;
            case PSObject:
                tableService.UpdateEntitiesInTable(Entity.Cast<PSObject>());
                break;
            default:
                throw new ArgumentException($"Entities provided were not Hashtable or PSObject! First entity was of type {Entity.GetType().FullName}!");
        }
    }
}
