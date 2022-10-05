using PipeHow.AzBobbyTables.Validation;
using System.Collections;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets
{
    /// <summary>
    /// <para type="synopsis">Add one or more entities to an Azure Table.</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "AzDataTableEntity")]
    [Alias("Add-AzDataTableRow")]
    public class AddAzDataTableEntity : AzDataTableCommandBase
    {
        /// <summary>
        /// <para type="description">The entities to add to the table.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "ConnectionString", ValueFromPipeline = true, Position = 1)]
        [Parameter(Mandatory = true, ParameterSetName = "SAS", ValueFromPipeline = true, Position = 1)]
        [Parameter(Mandatory = true, ParameterSetName = "Key", ValueFromPipeline = true, Position = 1)]
        [Parameter(Mandatory = true, ParameterSetName = "Token", ValueFromPipeline = true, Position = 1)]
        [Parameter(Mandatory = true, ParameterSetName = "ManagedIdentity", ValueFromPipeline = true, Position = 1)]
        [Alias("Row", "Entry", "Property")]
        [ValidateEntity]
        public Hashtable[] Entity { get; set; }

        /// <summary>
        /// <para type="description">Overwrites provided entities if they exist.</para>
        /// </summary>
        [Parameter(ParameterSetName = "ConnectionString")]
        [Parameter(ParameterSetName = "SAS")]
        [Parameter(ParameterSetName = "Key")]
        [Parameter(ParameterSetName = "Token")]
        [Parameter(ParameterSetName = "ManagedIdentity")]
        [Alias("UpdateExisting")]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// The process step of the pipeline.
        /// </summary>
        protected override void ProcessRecord()
        {
            tableService.AddEntitiesToTable(Entity, Force.IsPresent);
        }
    }
}
