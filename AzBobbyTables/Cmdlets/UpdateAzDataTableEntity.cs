using PipeHow.AzBobbyTables.Validation;
using System.Collections;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets
{
    /// <summary>
    /// Update one or more entities in an Azure Table.
    /// </summary>
    [Cmdlet(VerbsData.Update, "AzDataTableEntity")]
    [Alias("Update-AzDataTableRow")]
    public class UpdateAzDataTableEntity : AzDataTableEntityCommandBase
    {
        /// <summary>
        /// <para type="description">The entities to update in the table.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "ConnectionString", ValueFromPipeline = true)]
        [Parameter(Mandatory = true, ParameterSetName = "SASToken", ValueFromPipeline = true)]
        [Parameter(Mandatory = true, ParameterSetName = "Key", ValueFromPipeline = true)]
        [ValidateEntity()]
        [Alias("Row", "Entry", "Property")]
        public Hashtable[] Entity { get; set; }

        protected override void ProcessRecord()
        {
            AzDataTableService.UpdateEntitiesInTable(Entity);
        }
    }
}
