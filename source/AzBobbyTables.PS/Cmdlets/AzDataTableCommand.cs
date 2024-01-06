using PipeHow.AzBobbyTables.Core;
using System.Management.Automation;
using System.Threading;

namespace PipeHow.AzBobbyTables.Cmdlets;

/// <summary>
/// The base class of the Azure Table commands, containing connection parameters.
/// </summary>
public class AzDataTableCommand : PSCmdlet
{
    private protected AzDataTableService tableService;

    private protected CancellationTokenSource cancellationTokenSource = new();

    // Cancel any operations if user presses CTRL + C
    protected override void StopProcessing() => cancellationTokenSource.Cancel();
}
