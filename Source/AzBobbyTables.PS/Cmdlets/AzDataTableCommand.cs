using PipeHow.AzBobbyTables.Core;
using System;
using System.Collections;
using System.Linq;
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

    /// <summary>
    /// The process step of the pipeline.
    /// </summary>
    protected override void BeginProcessing() => WriteDebug("ParameterSetName: " + ParameterSetName);

    // Cancel any operations if user presses CTRL + C
    protected override void StopProcessing() => cancellationTokenSource.Cancel();
}
