using System.Linq;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets;

/// <summary>
/// <para type="synopsis">Get one or more entities from an Azure Table.</para>
/// </summary>
[Cmdlet(VerbsCommon.Get, "AzDataTableEntity", DefaultParameterSetName = "TableOperation")]
public class GetAzDataTableEntity : AzDataTableOperationCommand
{
    /// <summary>
    /// <para type="description">The OData filter to use in the query.</para>
    /// </summary>
    [Parameter(ParameterSetName = "TableOperation")]
    public string Filter { get; set; }

    /// <summary>
    /// <para type="description">The properties to return for the entities.</para>
    /// </summary>
    [Parameter(ParameterSetName = "TableOperation")]
    public string[] Property { get; set; }

    /// <summary>
    /// <para type="description">The amount of entities to retrieve.</para>
    /// </summary>
    [Parameter(ParameterSetName = "TableOperation")]
    [Alias("Top", "Take")]
    public int? First { get; set; }

    /// <summary>
    /// <para type="description">The amount of entities to skip from the query result.</para>
    /// </summary>
    [Parameter(ParameterSetName = "TableOperation")]
    public int? Skip { get; set; }

    /// <summary>
    /// <para type="description">The names of one or more properties to sort by, in order.</para>
    /// </summary>
    [Parameter(ParameterSetName = "TableOperation")]
    public string[] Sort { get; set; }

    /// <summary>
    /// <para type="description">Specify that the output should only specify the number of entities.</para>
    /// </summary>
    [Parameter(ParameterSetName = "Count", Mandatory = true)]
    public SwitchParameter Count { get; set; }

    protected override void BeginProcessing()
    {
        base.BeginProcessing();

        if (MyInvocation.BoundParameters.ContainsKey("Sort"))
        {
            WriteWarning("Using the Sort parameter with large data sets may result in slow queries.");
        }
    }

    /// <summary>
    /// The process step of the pipeline.
    /// </summary>
    protected override void ProcessRecord()
    {
        if (Count.IsPresent)
        {
            var entities = tableService.GetEntitiesFromTable(Filter, new [] { "PartitionKey", "RowKey" }, null, null, null);
            WriteObject(entities.Count());
        }
        else
        {
            var entities = tableService.GetEntitiesFromTable(Filter, Property, First, Skip, Sort);
            foreach (var entity in entities)
            {
                WriteObject(entity);
            }
        }
    }
}
