using System;

namespace PipeHow.AzBobbyTables.Core.Logging;

/// <summary>
/// A structured log record emitted from Core. Uses BCL-only types so that events can cross the
/// custom <see cref="System.Runtime.Loader.AssemblyLoadContext"/> that Core is loaded into
/// without dragging in System.Management.Automation on the Core side of the boundary.
/// </summary>
public sealed class PSLogEvent
{
    /// <summary>The severity of the event; drives which PowerShell stream the adapter picks.</summary>
    public PSLogLevel Level { get; }

    /// <summary>Human-readable message. Never null; empty is allowed but discouraged.</summary>
    public string Message { get; }

    /// <summary>
    /// Optional stable identifier for the event. Maps to the ErrorRecord's FullyQualifiedErrorId
    /// on the Error path and to a tag on the other streams so callers can filter reliably.
    /// </summary>
    public string? Code { get; }

    /// <summary>
    /// Optional free-form context such as a partition or row key. Kept as a string so callers
    /// don't have to depend on Core's entity types.
    /// </summary>
    public string? Context { get; }

    public PSLogEvent(PSLogLevel level, string message, string? code = null, string? context = null)
    {
        Level = level;
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Code = code;
        Context = context;
    }
}
