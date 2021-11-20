using PipeHow.AzBobbyTables.Validation;
using System.Collections;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets
{
    /// <summary>
    /// Add an entity to an Azure Table.
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "AzDataTableEntity")]
    [Alias("Add-AzDataTableRow")]
    public class AddAzDataTableEntity : AzDataTableEntityCommandBase
    {
        /// <summary>
        /// <para type="description">The entities to add to the table.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "ConnectionString", ValueFromPipeline = true)]
        [Parameter(Mandatory = true, ParameterSetName = "SASToken", ValueFromPipeline = true)]
        [Parameter(Mandatory = true, ParameterSetName = "Key", ValueFromPipeline = true)]
        [ValidateEntity()]
        [Alias("Row", "Entry", "Property")]
        public Hashtable[] Entity { get; set; }

        /// <summary>
        /// <para type="description">Overwrites provided entities if they exist.</para>
        /// </summary>
        [Parameter(ParameterSetName = "ConnectionString")]
        [Parameter(ParameterSetName = "SASToken")]
        [Parameter(ParameterSetName = "Key")]
        [Alias("UpdateExisting")]
        public SwitchParameter Force { get; set; }

        protected override void ProcessRecord()
        {
            AzDataTableService.AddEntitiesToTable(Entity, Force.IsPresent);
        }
    }
}
