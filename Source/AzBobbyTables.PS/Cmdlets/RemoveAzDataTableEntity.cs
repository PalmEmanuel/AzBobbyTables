using PipeHow.AzBobbyTables.Core;
using PipeHow.AzBobbyTables.Validation;
using System.Collections;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets
{
    /// <summary>
    /// <para type="synopsis">Remove one or more entities from an Azure Table.</para>
    /// <para type="description">Remove one or more entities from an Azure Table, based on PartitionKey and RowKey.</para>
    /// <example>
    ///     <code>
    ///$Entity = [pscustomobject]@{ PartitionKey = 'Example'; RowKey = '1' }
    ///Remove-AzDataTableEntity -Entity $Entity -TableName $TableName -StorageAccountName $Name -StorageAccountKey $Key
    ///     </code>
    ///     <para>Remove the entity with PartitionKey "Example" and RowKey "1" using the storage account name and an access key.</para>
    /// </example>
    /// </summary>
    /// <example>
    ///     <code>
    ///$UserEntity = Get-AzDataTableEntity -Filter "FirstName eq 'Bobby' and LastName eq 'Tables'" -TableName $TableName -ConnectionString $ConnectionString
    ///Remove-AzDataTableEntity -Entity $UserEntity -TableName $TableName -StorageAccountName $Name -StorageAccountKey $Key
    ///     </code>
    ///     <para>Get the user "Bobby Tables" from the table using a connection string, then remove the user using the storage account name and an access key.</para>
    /// </example>
    [Cmdlet(VerbsCommon.Remove, "AzDataTableEntity")]
    [Alias("Remove-AzDataTableRow")]
    public class RemoveAzDataTableEntity : AzDataTableEntityCommandBase
    {
        /// <summary>
        /// <para type="description">The entities to remove from the table.</para>
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
            AzDataTableService.RemoveEntitiesFromTable(Entity);
        }
    }
}
