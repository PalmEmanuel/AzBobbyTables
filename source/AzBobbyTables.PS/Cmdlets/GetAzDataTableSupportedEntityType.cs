using PipeHow.AzBobbyTables.Core;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets;

/// <summary>
/// <para type="synopsis">Get the list of supported entity data types for Azure Table operations.</para>
/// </summary>
[Cmdlet(VerbsCommon.Get, "AzDataTableSupportedEntityType")]
public class GetAzDataTableSupportedEntityType : PSCmdlet
{
    /// <summary>
    /// The process step of the pipeline.
    /// </summary>
    protected override void ProcessRecord()
    {
        var supportedTypes = AzDataTableService.GetSupportedEntityTypes();
        foreach (var type in supportedTypes)
        {
            WriteObject(type);
        }
    }
}
