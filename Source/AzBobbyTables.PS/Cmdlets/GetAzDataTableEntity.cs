using PipeHow.AzBobbyTables.Core;
using System.Collections;
using System.Linq;
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
            // Format back to to PSObject
            var entities = AzDataTableService.GetEntitiesFromTable(Filter).Select(e =>
            {
                Hashtable hashtable = new Hashtable();
                foreach (string key in e.Keys)
                {
                    hashtable.Add(key, e[key]);
                }
                return hashtable;
            });
            foreach (var entity in entities)
            {
                WriteObject(entity);
            }
        }
    }
}
