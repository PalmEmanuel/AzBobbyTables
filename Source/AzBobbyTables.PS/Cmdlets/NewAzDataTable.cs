using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets;

/// <summary>
/// <para type="synopsis">Create a new Azure Table.</para>
/// </summary>
[Cmdlet(VerbsCommon.New, "AzDataTable")]
public class NewAzDataTable : AzDataTableOperationCommand
{
    /// <summary>
    /// <para type="description">The context used for the table, created with New-AzDataTableContext.</para>
    /// </summary>
    [Parameter(Mandatory = true)]
    public AzDataTableContext Context { get; set; }

    // This command creates a table, logic to do so is in base class
}
