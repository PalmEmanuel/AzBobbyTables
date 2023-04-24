using System;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables;

public class AzDataTableException : Exception
{
    public ErrorRecord ErrorRecord { get; }

    public AzDataTableException(ErrorRecord errorRecord) => ErrorRecord = errorRecord;
}
