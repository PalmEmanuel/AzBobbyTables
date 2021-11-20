using System.Collections;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets
{
    /// <summary>
    /// Get one or more entities from an Azure Table.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "AzDataTableEntity")]
    [Alias("Get-AzDataTableRow")]
    public class GetAzDataTableEntity : AzDataTableEntityCommandBase
    {
        /// <summary>
        /// <para type="description">The OData filter to use in the query.</para>
        /// </summary>
        [Parameter(ParameterSetName = "ConnectionString", ValueFromPipeline = true)]
        [Parameter(ParameterSetName = "SASToken", ValueFromPipeline = true)]
        [Parameter(ParameterSetName = "Key", ValueFromPipeline = true)]
        [Alias("Query")]
        public string Filter { get; set; }

        protected override void ProcessRecord()
        {
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
