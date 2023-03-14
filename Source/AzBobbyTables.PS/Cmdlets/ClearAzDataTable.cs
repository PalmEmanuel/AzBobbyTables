using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets;

/// <summary>
/// <para type="synopsis">Clear all entities from an Azure Table.</para>
/// </summary>
[Cmdlet(VerbsCommon.Clear, "AzDataTable")]
public class ClearAzDataTable : AzDataTableOperationCommand
{
    protected override void EndProcessing() => tableService.ClearTable();
}
