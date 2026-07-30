using System;
using System.Linq;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets;

/// <summary>
/// <para type="synopsis">Get one or more entities from an Azure Table, reassembling entities that were split across multiple properties or rows by Add-AzDataTableLargeEntity.</para>
/// </summary>
[Cmdlet(VerbsCommon.Get, "AzDataTableLargeEntity", DefaultParameterSetName = "TableOperation")]
[OutputType(typeof(PSObject))]
public class GetAzDataTableLargeEntity : AzDataTableOperationCommand
{
    /// <summary>
    /// <para type="description">The context used for the table, created with New-AzDataTableContext.</para>
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "TableOperation")]
    [Parameter(Mandatory = true, ParameterSetName = "Count")]
    public AzDataTableContext Context { get; set; }

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
    /// <para type="description">The amount of physical rows to retrieve, counted before split entities are reassembled.</para>
    /// </summary>
    [Parameter(ParameterSetName = "TableOperation")]
    [Alias("Top", "Take")]
    public int? First { get; set; }

    /// <summary>
    /// <para type="description">The amount of physical rows to skip from the query result, counted before split entities are reassembled.</para>
    /// </summary>
    [Parameter(ParameterSetName = "TableOperation")]
    public int? Skip { get; set; }

    /// <summary>
    /// <para type="description">The names of one or more properties to sort by, in order.</para>
    /// </summary>
    [Parameter(ParameterSetName = "TableOperation")]
    public string[] Sort { get; set; }

    /// <summary>
    /// <para type="description">Specify that the output should only specify the number of entities. Counts physical rows, so parts of split entities count individually.</para>
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "Count")]
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
        if (tableService is null)
        {
            WriteError(new ErrorRecord(new InvalidOperationException("Could not establish connection!"), "ConnectionError", ErrorCategory.ConnectionError, null));
            return;
        }

        try
        {
            if (Count.IsPresent)
            {
                WriteObject(tableService.CountEntitiesInTable(Filter));
            }
            else
            {
                var entities = tableService.GetLargeEntitiesFromTable(Filter, Property, First, Skip, Sort, WriteWarning);
                foreach (var entity in entities)
                {
                    WriteObject(entity);
                }
            }
        }
        catch (AzDataTableException ex)
        {
            WriteError(ex.ErrorRecord);
        }
    }
}
