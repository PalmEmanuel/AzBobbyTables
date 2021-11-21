using System;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets
{
    /// <summary>
    /// The base class of the Azure Table commands, containing connection parameters.
    /// </summary>
    public class AzDataTableEntityCommandBase : PSCmdlet
    {
        /// <summary>
        /// <para type="description">The connection string to the storage account.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "ConnectionString")]
        [ValidateNotNullOrEmpty()]
        public string ConnectionString { get; set; }

        /// <summary>
        /// <para type="description">The name of the table.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "ConnectionString")]
        [Parameter(Mandatory = true, ParameterSetName = "Key")]
        [ValidateNotNullOrEmpty()]
        public string TableName { get; set; }

        /// <summary>
        /// <para type="description">The storage account access key.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "Key")]
        [ValidateNotNullOrEmpty()]
        public string StorageAccountKey { get; set; }

        /// <summary>
        /// <para type="description">The table endpoint of the storage account.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "Key")]
        [ValidateNotNullOrEmpty()]
        public Uri TableEndpoint { get; set; }

        /// <summary>
        /// <para type="description">The shared access signature to the storage account.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "SAS")]
        [Alias("SAS")]
        [ValidateNotNullOrEmpty()]
        public Uri SharedAccessSignature { get; set; }

        /// <summary>
        /// The process step of the pipeline.
        /// </summary>
        protected override void BeginProcessing()
        {
            switch (ParameterSetName)
            {
                case "ConnectionString":
                    AzDataTableService.Connect(ConnectionString, TableName);
                    break;
                case "SAS":
                    AzDataTableService.Connect(SharedAccessSignature);
                    break;
                case "Key":
                    AzDataTableService.Connect(TableEndpoint, TableName, StorageAccountKey);
                    break;
                default:
                    throw new ArgumentException($"Unknown parameter set '{ParameterSetName}' was used!");
            }
        }
    }
}
