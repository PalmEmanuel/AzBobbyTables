using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets
{
    /// <summary>
    /// <para type="synopsis">Get one or more entities from an Azure Table.</para>
    /// <para type="description">Get either all entities from an Azure Table, or those matching a provided OData filter.</para>
    /// <example>
    ///     <code>
    ///$UserEntity = Get-AzDataTableEntity -Filter "FirstName eq 'Bobby' and LastName eq 'Tables'" -TableName $TableName -ConnectionString $ConnectionString
    ///     </code>
    ///     <para>Get the user "Bobby Tables" from the table using a connection string.</para>
    /// </example>
    /// <para type="link" uri="https://docs.microsoft.com/en-gb/rest/api/storageservices/querying-tables-and-entities">Documentation on querying tables and entities.</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "AzDataTableEntity")]
    [Alias("Get-AzDataTableRow")]
    public class GetAzDataTableEntity : AzDataTableEntityCommandBase
    {
        /// <summary>
        /// <para type="description">The OData filter to use in the query.</para>
        /// <para type="link" uri="https://docs.microsoft.com/en-gb/rest/api/storageservices/querying-tables-and-entities">Documentation on querying tables and entities.</para>
        /// </summary>
        [Parameter(ParameterSetName = "ConnectionString", ValueFromPipeline = true)]
        [Parameter(ParameterSetName = "SAS", ValueFromPipeline = true)]
        [Parameter(ParameterSetName = "Key", ValueFromPipeline = true)]
        [Alias("Query")]
        public string Filter { get; set; }

        /// <summary>
        /// The process step of the pipeline.
        /// </summary>
        protected override void ProcessRecord()
        {
            // Get all entities from table, loop through them and output them as PS(Custom)Objects
            foreach (var entity in AzDataTableService.GetEntitiesFromTable(Filter))
            {
                PSObject entityObject = new PSObject();
                entityObject.Properties.Add(new PSNoteProperty("ETag", entity.ETag));
                foreach (var key in entity.Keys)
                {
                    if (key == "odata.etag") continue;
                    entityObject.Properties.Add(new PSNoteProperty(key, entity[key]));
                }
                WriteObject(entityObject);
            }
        }
    }
}
