using System;
using System.Linq;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets;

/// <summary>
/// <para type="synopsis">Get one or more Azure Tables.</para>
/// </summary>
[Cmdlet(VerbsCommon.Get, "AzDataTable")]
[OutputType(typeof(PSObject))]
public class GetAzDataTable : AzDataTableOperationCommand
{
    /// <summary>
    /// <para type="description">The context used for the table, created with New-AzDataTableContext.</para>
    /// </summary>
    [Parameter(Mandatory = true)]
    public AzDataTableContext Context { get; set; }

    /// <summary>
    /// <para type="description">The filter to use in the query.</para>
    /// </summary>
    [Parameter()]
    [ValidateNotNullOrEmpty]
    public string Filter { get; set; }

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

        if (!string.IsNullOrWhiteSpace(Context.TableName))
        {
            WriteWarning("Table name exists in the context but is not used in this command. To verify the existence of a specific table, use the -Filter parameter.");
        }

        string filterMessage = "Finding ";
        if (MyInvocation.BoundParameters.ContainsKey("Filter"))
        {
            filterMessage += $"all tables matching filter '{Filter}'.";
        }
        else
        {
            filterMessage += "all tables on storage account in provided context.";
        }
        WriteVerbose(filterMessage);

        try
        {
            var tables = tableService.GetTables(Filter);
            foreach (var table in tables)
            {
                WriteObject(table);
            }
        }
        catch (AzDataTableException ex)
        {
            WriteError(ex.ErrorRecord);
        }
    }
}
