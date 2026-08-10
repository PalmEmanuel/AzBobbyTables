using PipeHow.AzBobbyTables.Core;
using PipeHow.AzBobbyTables.Logging;
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

    // Intentionally run as after BeginProcessing() in child class to hook up logging
    protected override void BeginProcessing()
    {
        base.BeginProcessing();

        // Attach the cmdlet-side log sink so Core diagnostics land on the pipeline's PowerShell
        // streams. Set after construction because the Core factory methods intentionally know
        // nothing about System.Management.Automation. Not every command builds a tableService
        // (e.g. New-AzDataTableContext only produces a context object), so guard against null.
        if (tableService is not null)
        {
            tableService.LogSink = PSCmdletLogSink.Create(this);
        }
    }
}
