using System;

namespace PipeHow.AzBobbyTables.Core.Logging;

/// <summary>
/// A sink that forwards each event to a caller-supplied delegate. Lives in Core so that no type
/// in the AzBobbyTables.PS assembly needs to implement <see cref="IPSLogSink"/> directly.
/// </summary>
/// <remarks>
/// This matters for module loading: PowerShell enumerates every type in the root module assembly
/// during <c>Import-Module</c>, and loading a type's metadata eagerly resolves its base class and
/// implemented interfaces. If a PS-side type implemented <see cref="IPSLogSink"/>, Core would have
/// to load before the module's <c>IModuleAssemblyInitializer.OnImport</c> hook has a chance to
/// register the dependency <see cref="System.Runtime.Loader.AssemblyLoadContext"/> — and Core lives
/// in that ALC, so the load would fail. Method bodies aren't scanned during type enumeration, so a
/// delegate captured in cmdlet code is safe.
/// </remarks>
public sealed class DelegateLogSink : IPSLogSink
{
    private readonly Action<PSLogEvent> _handler;

    public DelegateLogSink(Action<PSLogEvent> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public void Log(PSLogEvent logEvent)
    {
        if (logEvent is null) return;
        _handler(logEvent);
    }
}
