using System.Collections;
using System.Linq;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets
{
    /// <summary>
    /// <para type="synopsis">Get one or more entities from an Azure Table.</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "AzDataTableEntity")]
    [Alias("Get-AzDataTableRow")]
    public class GetAzDataTableEntity : AzDataTableCommandBase
    {
        /// <summary>
        /// <para type="description">The OData filter to use in the query.</para>
        /// </summary>
        [Parameter(ParameterSetName = "ConnectionString", ValueFromPipeline = true, Position = 1)]
        [Parameter(ParameterSetName = "SAS", ValueFromPipeline = true, Position = 1)]
        [Parameter(ParameterSetName = "Key", ValueFromPipeline = true, Position = 1)]
        [Parameter(ParameterSetName = "Token", ValueFromPipeline = true, Position = 1)]
        [Parameter(ParameterSetName = "ManagedIdentity", ValueFromPipeline = true, Position = 1)]
        [Alias("Query")]
        public string Filter { get; set; }

        /// <summary>
        /// <para type="description">The properties to return for the entities.</para>
        /// </summary>
        [Parameter(ParameterSetName = "ConnectionString", ValueFromPipeline = true, Position = 2)]
        [Parameter(ParameterSetName = "SAS", ValueFromPipeline = true, Position = 2)]
        [Parameter(ParameterSetName = "Key", ValueFromPipeline = true, Position = 2)]
        [Parameter(ParameterSetName = "Token", ValueFromPipeline = true, Position = 2)]
        [Parameter(ParameterSetName = "ManagedIdentity", ValueFromPipeline = true, Position = 2)]
        public string[] Property { get; set; }

        /// <summary>
        /// The process step of the pipeline.
        /// </summary>
        protected override void ProcessRecord()
        {
            // Format back to to PSObject
            var entities = tableService.GetEntitiesFromTable(Filter, Property).Select(e =>
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
