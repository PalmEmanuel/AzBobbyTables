using PipeHow.AzBobbyTables.Validation;
using System;
using System.Collections;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets
{
    /// <summary>
    /// Remove an entity from an Azure Table.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "RemoveDataTableEntity")]
    [Alias("Remove-AzDataTableRow")]
    public class RemoveAzDataTableEntity : PSCmdlet
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
        [ValidateNotNullOrEmpty()]
        public string TableName { get; set; }

        /// <summary>
        /// <para type="description">The Shared Access Signature token to the storage account.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "SASToken")]
        [ValidateNotNullOrEmpty()]
        public Uri SASToken { get; set; }

        /// <summary>
        /// <para type="description">The entities to remove from the table.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "ConnectionString", ValueFromPipeline = true)]
        [Parameter(Mandatory = true, ParameterSetName = "SASToken", ValueFromPipeline = true)]
        [ValidateEntity()]
        [Alias("Row", "Entry", "Property")]
        public Hashtable[] Entity { get; set; }

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
                default:
                    throw new ArgumentException($"Unknown parameter set '{ParameterSetName}' was used!");
            }
        }

        protected override void ProcessRecord()
        {
            AzDataTableService.RemoveEntitiesFromTable(Entity);
        }
    }
}
