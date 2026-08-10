using System;
using System.Management.Automation;
using PipeHow.AzBobbyTables.Core.Logging;

namespace PipeHow.AzBobbyTables.Logging;

/// <summary>
/// Maps a <see cref="PSLogEvent"/> onto the PowerShell stream methods of a <see cref="Cmdlet"/>.
/// </summary>
/// <remarks>
/// <para>
/// Intentionally does <b>not</b> implement <see cref="IPSLogSink"/> — that interface lives in
/// AzBobbyTables.Core, and implementing it here would force Core to load during PowerShell's
/// cmdlet type enumeration, before <see cref="System.Management.Automation.IModuleAssemblyInitializer.OnImport"/>
/// registers the dependency ALC resolver. Callers wire this up through
/// <see cref="DelegateLogSink"/> instead, which captures the mapping as a delegate.
/// </para>
/// <para>
/// The wrapped cmdlet's stream writers are only safe to call from the pipeline thread, so this
/// adapter must only be attached to a service invoked synchronously on that same thread. The
/// constraint is documented on <see cref="IPSLogSink"/>.
/// </para>
/// </remarks>
public static class PSCmdletLogSink
{
    /// <summary>
    /// Build a Core-side <see cref="DelegateLogSink"/> that forwards events to <paramref name="cmdlet"/>'s streams.
    /// </summary>
    public static IPSLogSink Create(Cmdlet cmdlet)
    {
        if (cmdlet is null) throw new ArgumentNullException(nameof(cmdlet));
        return new DelegateLogSink(logEvent => Write(cmdlet, logEvent));
    }

    /// <summary>Write a single event to the matching stream on <paramref name="cmdlet"/>.</summary>
    public static void Write(Cmdlet cmdlet, PSLogEvent logEvent)
    {
        if (cmdlet is null) throw new ArgumentNullException(nameof(cmdlet));
        if (logEvent is null) return;

        switch (logEvent.Level)
        {
            case PSLogLevel.Verbose:
                cmdlet.WriteVerbose(logEvent.Message);
                break;
            case PSLogLevel.Warning:
                cmdlet.WriteWarning(logEvent.Message);
                break;
            case PSLogLevel.Debug:
                cmdlet.WriteDebug(logEvent.Message);
                break;
            case PSLogLevel.Information:
                // Tag the record with the event Code (or a default) so downstream Information
                // stream consumers can filter on it. `Source` gets the code as well so that any
                // consumer relying on either surface can find it, and PowerShell's own message
                // formatting picks up on it.
                var tag = string.IsNullOrEmpty(logEvent.Code) ? "AzBobbyTables" : logEvent.Code!;
                var infoRecord = new InformationRecord(logEvent.Message, tag);
                infoRecord.Tags.Add(tag);
                cmdlet.WriteInformation(infoRecord);
                break;
            case PSLogLevel.Error:
                // The ErrorRecord requires an exception; wrap the message rather than throwing
                // ourselves so the caller keeps control of pipeline flow. Code becomes the
                // FullyQualifiedErrorId, which is what PowerShell users key error handling on.
                // ErrorCategory intentionally stays NotSpecified: the Core log contract keeps
                // events to level + message + code + context by design (the issue spec calls this
                // out), and a stable Code is enough for consumers to categorise errors.
                var errorId = string.IsNullOrEmpty(logEvent.Code) ? "AzBobbyTablesError" : logEvent.Code!;
                var record = new ErrorRecord(
                    new InvalidOperationException(logEvent.Message),
                    errorId,
                    ErrorCategory.NotSpecified,
                    logEvent.Context);
                cmdlet.WriteError(record);
                break;
        }
    }
}
