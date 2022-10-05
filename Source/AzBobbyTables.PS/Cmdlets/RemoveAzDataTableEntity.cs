using PipeHow.AzBobbyTables.Validation;
using System.Collections;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets
{
    /// <summary>
    /// <para type="synopsis">Remove one or more entities from an Azure Table.</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "AzDataTableEntity")]
    [Alias("Remove-AzDataTableRow")]
    public class RemoveAzDataTableEntity : AzDataTableCommandBase
    {
        /// <summary>
        /// <para type="description">The entities to remove from the table.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "ConnectionString", ValueFromPipeline = true, Position = 1)]
        [Parameter(Mandatory = true, ParameterSetName = "SAS", ValueFromPipeline = true, Position = 1)]
        [Parameter(Mandatory = true, ParameterSetName = "Key", ValueFromPipeline = true, Position = 1)]
        [Parameter(Mandatory = true, ParameterSetName = "Token", ValueFromPipeline = true, Position = 1)]
        [Parameter(Mandatory = true, ParameterSetName = "ManagedIdentity", ValueFromPipeline = true, Position = 1)]
        [ValidateEntity()]
        [Alias("Row", "Entry", "Property")]
        public Hashtable[] Entity { get; set; }

        /// <summary>
        /// The process step of the pipeline.
        /// </summary>
        protected override void ProcessRecord()
        {
            tableService.RemoveEntitiesFromTable(Entity);
        }
    }
}
