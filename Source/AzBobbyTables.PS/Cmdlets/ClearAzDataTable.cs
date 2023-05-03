using System;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets;

/// <summary>
/// <para type="synopsis">Clear all entities from an Azure Table.</para>
/// </summary>
[Cmdlet(VerbsCommon.Clear, "AzDataTable")]
public class ClearAzDataTable : AzDataTableOperationCommand
{
    /// <summary>
    /// <para type="description">The context used for the table, created with New-AzDataTableContext.</para>
    /// </summary>
    [Parameter(Mandatory = true)]
    public AzDataTableContext Context { get; set; }

    protected override void EndProcessing()
    {
        if (tableService is null)
        {
            WriteError(new ErrorRecord(new InvalidOperationException("Could not establish connection!"), "ConnectionError", ErrorCategory.ConnectionError, null));
            return;
        }

        try
        {
            tableService.ClearTable();
        }
        catch (AzDataTableException ex)
        {
            WriteError(ex.ErrorRecord);
        }
    }
}
