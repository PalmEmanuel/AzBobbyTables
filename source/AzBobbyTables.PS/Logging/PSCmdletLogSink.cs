using System;
using System.Management.Automation;
using PipeHow.AzBobbyTables.Core.Logging;

namespace PipeHow.AzBobbyTables.Logging;

/// <summary>
/// Adapter that receives <see cref="PSLogEvent"/> instances from Core and forwards them to the
/// PowerShell stream methods of a <see cref="Cmdlet"/>. This lives in the cmdlet assembly on
/// purpose so that AzBobbyTables.Core can stay free of System.Management.Automation for the
/// logging contract itself.
/// </summary>
/// <remarks>
/// The wrapped cmdlet's stream writers are only safe to call from the pipeline thread, so this
/// adapter must only be attached to a service invoked synchronously on that same thread. Core
/// respects that today; the constraint is documented on <see cref="IPSLogSink"/>.
/// </remarks>
public sealed class PSCmdletLogSink : IPSLogSink
{
    private readonly Cmdlet _cmdlet;

    public PSCmdletLogSink(Cmdlet cmdlet)
    {
        _cmdlet = cmdlet ?? throw new ArgumentNullException(nameof(cmdlet));
    }

    public void Log(PSLogEvent logEvent)
    {
        if (logEvent is null) return;

        switch (logEvent.Level)
        {
            case PSLogLevel.Verbose:
                _cmdlet.WriteVerbose(logEvent.Message);
                break;
            case PSLogLevel.Warning:
                _cmdlet.WriteWarning(logEvent.Message);
                break;
            case PSLogLevel.Debug:
                _cmdlet.WriteDebug(logEvent.Message);
                break;
            case PSLogLevel.Information:
                // Tag the record with the event Code (or a default) so downstream Information
                // stream consumers can filter on it. `Source` gets the code as well so that any
                // consumer relying on either surface can find it, and PowerShell's own message
                // formatting picks up on it.
                var tag = string.IsNullOrEmpty(logEvent.Code) ? "AzBobbyTables" : logEvent.Code!;
                var infoRecord = new InformationRecord(logEvent.Message, tag);
                infoRecord.Tags.Add(tag);
                _cmdlet.WriteInformation(infoRecord);
                break;
            case PSLogLevel.Error:
                // The ErrorRecord requires an exception; wrap the message rather than throwing
                // ourselves so the caller keeps control of pipeline flow. Code becomes the
                // FullyQualifiedErrorId, which is what PowerShell users key error handling on.
                var errorId = string.IsNullOrEmpty(logEvent.Code) ? "AzBobbyTablesError" : logEvent.Code!;
                var record = new ErrorRecord(
                    new InvalidOperationException(logEvent.Message),
                    errorId,
                    ErrorCategory.NotSpecified,
                    logEvent.Context);
                _cmdlet.WriteError(record);
                break;
        }
    }
}
