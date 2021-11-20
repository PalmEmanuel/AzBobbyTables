using PipeHow.AzBobbyTables.Validation;
using System.Collections;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets
{
    /// <summary>
    /// Remove one or more entities from an Azure Table.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "AzDataTableEntity")]
    [Alias("Remove-AzDataTableRow")]
    public class RemoveAzDataTableEntity : AzDataTableEntityCommandBase
    {
        /// <summary>
        /// <para type="description">The entities to remove from the table.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "ConnectionString", ValueFromPipeline = true)]
        [Parameter(Mandatory = true, ParameterSetName = "SASToken", ValueFromPipeline = true)]
        [Parameter(Mandatory = true, ParameterSetName = "Key", ValueFromPipeline = true)]
        [ValidateEntity()]
        [Alias("Row", "Entry", "Property")]
        public Hashtable[] Entity { get; set; }

        protected override void ProcessRecord()
        {
            AzDataTableService.RemoveEntitiesFromTable(Entity);
        }
    }
}
