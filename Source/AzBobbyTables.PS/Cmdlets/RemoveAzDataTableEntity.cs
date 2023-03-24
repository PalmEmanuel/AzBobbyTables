using PipeHow.AzBobbyTables.Validation;
using System;
using System.Collections;
using System.Linq;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets;

/// <summary>
/// <para type="synopsis">Remove one or more entities from an Azure Table.</para>
/// </summary>
[Cmdlet(VerbsCommon.Remove, "AzDataTableEntity")]
public class RemoveAzDataTableEntity : AzDataTableOperationCommand
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
    /// The process step of the pipeline.
    /// </summary>
    protected override void ProcessRecord()
    {
        switch (Entity.First())
        {
            case Hashtable:
                tableService.RemoveEntitiesFromTable(Entity.Cast<Hashtable>());
                break;
            case PSObject:
                tableService.RemoveEntitiesFromTable(Entity.Cast<PSObject>());
                break;
            default:
                throw new ArgumentException($"Entities provided were not Hashtable or PSObject! Entity was {Entity.GetType().FullName}");
        }
    }
}
