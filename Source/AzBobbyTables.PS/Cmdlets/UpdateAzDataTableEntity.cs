using PipeHow.AzBobbyTables.Core;
using PipeHow.AzBobbyTables.Validation;
using System.Collections;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets
{
    /// <summary>
    /// <para type="synopsis">Update one or more entities in an Azure Table.</para>
    /// <para type="description">Update one or more entities already existing in an Azure Table. For adding and overwriting, see the command Add-AzDataTableEntity.</para>
    /// <para type="description">The PartitionKey and RowKey cannot be updated.</para>
    /// <example>
    ///     <code>
    ///$UserEntity = Get-AzDataTableEntity -Filter "FirstName eq 'Bobby'" -TableName $TableName -ConnectionString $ConnectionString
    ///$UserEntity.LastName = 'Tables'
    ///Update-AzDataTableEntity -Entity $UserEntity -TableName $TableName -ConnectionString $ConnectionString
    ///     </code>
    ///     <para>Update the last name of the user "Bobby" to "Tables" using a connection string.</para>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsData.Update, "AzDataTableEntity")]
    [Alias("Update-AzDataTableRow")]
    public class UpdateAzDataTableEntity : AzDataTableEntityCommandBase
    {
        /// <summary>
        /// <para type="description">The entities to update in the table.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "ConnectionString", ValueFromPipeline = true)]
        [Parameter(Mandatory = true, ParameterSetName = "SAS", ValueFromPipeline = true)]
        [Parameter(Mandatory = true, ParameterSetName = "Key", ValueFromPipeline = true)]
        [ValidateEntity()]
        [Alias("Row", "Entry", "Property")]
        public Hashtable[] Entity { get; set; }

        /// <summary>
        /// The process step of the pipeline.
        /// </summary>
        protected override void ProcessRecord()
        {
            AzDataTableService.UpdateEntitiesInTable(Entity);
        }
    }
}
