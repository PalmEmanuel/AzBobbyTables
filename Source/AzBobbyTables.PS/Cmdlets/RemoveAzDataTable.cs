using System;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets;

/// <summary>
/// <para type="synopsis">Removes a Azure Table.</para>
/// </summary>
[Cmdlet(VerbsCommon.Remove, "AzDataTable")]
public class RemoveAzDataTable : AzDataTableOperationCommand
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
            tableService.RemoveTable();
        }
        catch (AzDataTableException ex)
        {
            WriteError(ex.ErrorRecord);
        }
    }
}
