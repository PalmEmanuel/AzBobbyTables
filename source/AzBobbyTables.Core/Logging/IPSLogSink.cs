namespace PipeHow.AzBobbyTables.Core.Logging;

/// <summary>
/// Sink that receives log events emitted from Core operations. Implemented in the cmdlet layer
/// where PowerShell stream APIs are available. Core never depends on this being non-null; a
/// missing sink means "do not emit".
/// </summary>
/// <remarks>
/// Implementations must be safe to invoke synchronously from the thread driving the Core
/// operation. PowerShell stream writers are not safe to call from arbitrary worker threads, so
/// Core only emits from the pipeline thread today. If Core later introduces background
/// producers the sink contract will need buffering; that is intentionally deferred.
/// </remarks>
public interface IPSLogSink
{
    /// <summary>
    /// Handle a log event. Implementations should not throw for expected failure modes; any
    /// throw here will surface into the Core operation and abort it.
    /// </summary>
    void Log(PSLogEvent logEvent);
}
