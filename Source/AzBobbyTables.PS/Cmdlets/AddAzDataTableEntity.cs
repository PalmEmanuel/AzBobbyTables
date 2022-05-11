using PipeHow.AzBobbyTables.Core;
using PipeHow.AzBobbyTables.Validation;
using System.Collections;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets
{
    /// <summary>
    /// <para type="synopsis">Add one or more entities to an Azure Table.</para>
    /// <para type="description">Add an entity to an Azure Table, as a PSCustomObject.</para>
    /// <example>
    ///     <code>
    ///$User = [pscustomobject]@{ FirstName = 'Bobby'; LastName = 'Tables'; PartitionKey = 'Example'; RowKey = '1' }
    ///Add-AzDataTableEntity -Entity $User -TableName $TableName -SharedAccessSignature $SAS
    ///     </code>
    ///     <para>Add the user "Bobby Tables" to the table using a shared access signature URL.</para>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "AzDataTableEntity")]
    [Alias("Add-AzDataTableRow")]
    public class AddAzDataTableEntity : AzDataTableEntityCommandBase
    {
        /// <summary>
        /// <para type="description">The entities to add to the table.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "ConnectionString", ValueFromPipeline = true)]
        [Parameter(Mandatory = true, ParameterSetName = "SAS", ValueFromPipeline = true)]
        [Parameter(Mandatory = true, ParameterSetName = "Key", ValueFromPipeline = true)]
        [Alias("Row", "Entry", "Property")]
        [ValidateEntity]
        public Hashtable[] Entity { get; set; }

        /// <summary>
        /// <para type="description">Overwrites provided entities if they exist.</para>
        /// </summary>
        [Parameter(ParameterSetName = "ConnectionString")]
        [Parameter(ParameterSetName = "SAS")]
        [Parameter(ParameterSetName = "Key")]
        [Alias("UpdateExisting")]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// The process step of the pipeline.
        /// </summary>
        protected override void ProcessRecord()
        {
            AzDataTableService.AddEntitiesToTable(Entity, Force.IsPresent);
        }
    }
}
