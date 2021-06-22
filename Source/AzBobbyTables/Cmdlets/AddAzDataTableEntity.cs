using PipeHow.AzBobbyTables.Validation;
using System;
using System.Collections;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets
{
    /// <summary>
    /// Add an entity to an Azure Table.
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "AzDataTableEntity")]
    [Alias("Add-AzDataTableRow")]
    public class AddAzDataTableEntity : PSCmdlet
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
        /// <para type="description">The entities to add to the table.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "ConnectionString", ValueFromPipeline = true)]
        [Parameter(Mandatory = true, ParameterSetName = "SASToken", ValueFromPipeline = true)]
        [ValidateEntity()]
        [Alias("Row", "Entry", "Property")]
        public Hashtable[] Entity { get; set; }

        /// <summary>
        /// <para type="description">Overwrites provided entities if they exist.</para>
        /// </summary>
        [Parameter(ParameterSetName = "ConnectionString")]
        [Parameter(ParameterSetName = "SASToken")]
        [Alias("UpdateExisting")]
        public SwitchParameter Force { get; set; }

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
            WriteObject(AzDataTableService.AddEntitiesToTable(Entity, Force.ToBool()));
        }
    }
}
