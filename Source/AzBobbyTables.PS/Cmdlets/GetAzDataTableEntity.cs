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
        /// <para type="description">The amount of entities to retrieve.</para>
        /// </summary>
        [Parameter(ParameterSetName = "ConnectionString", ValueFromPipeline = true, Position = 3)]
        [Parameter(ParameterSetName = "SAS", ValueFromPipeline = true, Position = 3)]
        [Parameter(ParameterSetName = "Key", ValueFromPipeline = true, Position = 3)]
        [Parameter(ParameterSetName = "Token", ValueFromPipeline = true, Position = 3)]
        [Parameter(ParameterSetName = "ManagedIdentity", ValueFromPipeline = true, Position = 3)]
        [Alias("Top", "Take")]
        public int? First { get; set; }

        /// <summary>
        /// <para type="description">The amount of entities to skip from the query result.</para>
        /// </summary>
        [Parameter(ParameterSetName = "ConnectionString", ValueFromPipeline = true, Position = 4)]
        [Parameter(ParameterSetName = "SAS", ValueFromPipeline = true, Position = 4)]
        [Parameter(ParameterSetName = "Key", ValueFromPipeline = true, Position = 4)]
        [Parameter(ParameterSetName = "Token", ValueFromPipeline = true, Position = 4)]
        [Parameter(ParameterSetName = "ManagedIdentity", ValueFromPipeline = true, Position = 4)]
        public int? Skip { get; set; }

        /// <summary>
        /// <para type="description">The names of one or more properties to sort by, in order.</para>
        /// </summary>
        [Parameter(ParameterSetName = "ConnectionString", ValueFromPipeline = true, Position = 5)]
        [Parameter(ParameterSetName = "SAS", ValueFromPipeline = true, Position = 5)]
        [Parameter(ParameterSetName = "Key", ValueFromPipeline = true, Position = 5)]
        [Parameter(ParameterSetName = "Token", ValueFromPipeline = true, Position = 5)]
        [Parameter(ParameterSetName = "ManagedIdentity", ValueFromPipeline = true, Position = 5)]
        public string[] Sort { get; set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            if (MyInvocation.BoundParameters.ContainsKey("Sort"))
            {
                WriteWarning("Using the Sort parameter with large data sets may result in slow queries.");
            }
        }

        /// <summary>
        /// The process step of the pipeline.
        /// </summary>
        protected override void ProcessRecord()
        {
            var entities = tableService.GetEntitiesFromTable(Filter, Property, First, Skip, Sort);
            foreach (var entity in entities)
            {
                WriteObject(entity);
            }
        }
    }
}
