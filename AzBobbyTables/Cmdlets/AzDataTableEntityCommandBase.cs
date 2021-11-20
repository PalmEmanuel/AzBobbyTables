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
        /// <para type="description">The name of the table.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "Key")]
        [ValidateNotNullOrEmpty()]
        public string StorageAccountKey { get; set; }

        /// <summary>
        /// <para type="description">The name of the table.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "Key")]
        [ValidateNotNullOrEmpty()]
        public Uri TableEndpoint { get; set; }

        /// <summary>
        /// <para type="description">The Shared Access Signature token to the storage account.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "SASToken")]
        [ValidateNotNullOrEmpty()]
        public Uri SASToken { get; set; }

        protected override void BeginProcessing()
        {
            switch (ParameterSetName)
            {
                case "ConnectionString":
                    AzDataTableService.Connect(ConnectionString, TableName);
                    break;
                case "SASToken":
                    AzDataTableService.Connect(SASToken);
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
