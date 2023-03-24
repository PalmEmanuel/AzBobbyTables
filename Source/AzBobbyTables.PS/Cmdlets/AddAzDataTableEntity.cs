using PipeHow.AzBobbyTables.Validation;
using System;
using System.Collections;
using System.Linq;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets;

/// <summary>
/// <para type="synopsis">Add one or more entities to an Azure Table.</para>
/// </summary>
[Cmdlet(VerbsCommon.Add, "AzDataTableEntity")]
public class AddAzDataTableEntity : AzDataTableOperationCommand
{
    /// <summary>
    /// <para type="description">The context used for the table, created with New-AzDataTableContext.</para>
    /// </summary>
    [Parameter(Mandatory = true)]
    public AzDataTableContext Context { get; set; }

    /// <summary>
    /// <para type="description">The entities to add to the table.</para>
    /// </summary>
    [Parameter(Mandatory = true, ValueFromPipeline = true)]
    [ValidateEntity]
    [ValidateNotNullOrEmpty()]
    public object[] Entity { get; set; }

    /// <summary>
    /// <para type="description">Overwrites provided entities if they exist.</para>
    /// </summary>
    [Parameter()]
    public SwitchParameter Force { get; set; }

    /// <summary>
    /// <para type="description">If the table should be created if it does not exist.</para>
    /// </summary>
    [Parameter()]
    public SwitchParameter CreateTableIfNotExists { get; set; }

    /// <summary>
    /// The process step of the pipeline.
    /// </summary>
    protected override void ProcessRecord() {
        switch (Entity.First())
        {
            case Hashtable:
                tableService.AddEntitiesToTable(Entity.Cast<Hashtable>(), Force.IsPresent);
                break;
            case PSObject:
                tableService.AddEntitiesToTable(Entity.Cast<PSObject>(), Force.IsPresent);
                break;
            default:
                throw new ArgumentException($"Entities provided were not Hashtable or PSObject! First entity was of type {Entity.GetType().FullName}!");
        }
    }
}
