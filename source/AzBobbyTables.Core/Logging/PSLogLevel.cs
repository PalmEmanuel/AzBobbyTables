namespace PipeHow.AzBobbyTables.Core.Logging;

/// <summary>
/// Log severity levels emitted by <see cref="AzDataTableService"/>. Named to align with the
/// PowerShell streams they map to in the cmdlet-side adapter, so the Core assembly can describe
/// intent without depending on System.Management.Automation.
/// </summary>
public enum PSLogLevel
{
    Verbose,
    Warning,
    Debug,
    Information,
    Error
}
