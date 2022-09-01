using PipeHow.AzBobbyTables.Core;
using PipeHow.AzBobbyTables.Validation;
using System.Collections;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets
{
    /// <summary>
    /// <para type="synopsis">Clear all entities from an Azure Table.</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Clear, "AzDataTable")]
    public class ClearAzDataTable : AzDataTableCommandBase
    {
        /// <summary>
        /// The end step of the pipeline.
        /// </summary>
        ///
        protected override void EndProcessing()
        {
            tableService.ClearTable();
        }
    }
}
