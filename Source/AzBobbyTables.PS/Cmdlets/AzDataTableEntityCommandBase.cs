using PipeHow.AzBobbyTables.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets
{
    /// <summary>
    /// The base class of the Azure Table commands, containing connection parameters.
    /// </summary>
    public class AzDataTableEntityCommandBase : PSCmdlet
    {
        /// <summary>
        /// <para type="description">The name of the table.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "ConnectionString")]
        [Parameter(Mandatory = true, ParameterSetName = "SAS")]
        [Parameter(Mandatory = true, ParameterSetName = "Key")]
        [ValidateNotNullOrEmpty()]
        public string TableName { get; set; }

        /// <summary>
        /// <para type="description">The connection string to the storage account.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "ConnectionString")]
        [ValidateNotNullOrEmpty()]
        public string ConnectionString { get; set; }

        /// <summary>
        /// <para type="description">The name of the storage account.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "Key")]
        [ValidateNotNullOrEmpty()]
        public string StorageAccountName { get; set; }

        /// <summary>
        /// <para type="description">The storage account access key.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "Key")]
        [ValidateNotNullOrEmpty()]
        public string StorageAccountKey { get; set; }

        /// <summary>
        /// <para type="description">The table service SAS URL.</para>
        /// <para type="description">The table endpoint of the storage account, with the shared access token token appended to it.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "SAS")]
        [Alias("SAS")]
        [ValidateNotNullOrEmpty()]
        [ValidatePattern("https://.*")]
        public Uri SharedAccessSignature { get; set; }

        /// <summary>
        /// The process step of the pipeline.
        /// </summary>
        protected override void BeginProcessing()
        {
            WriteDebug("ParameterSetName: " + ParameterSetName);
            
            if (MyInvocation.BoundParameters.ContainsKey("Entity"))
            {
                Hashtable[] entities = MyInvocation.BoundParameters["Entity"] as Hashtable[];

                // Cast hashtable to dictionary to be able to run linq
                // Then find if any values have unsupported types
                var dictionaries = entities.Select(e => e.ToDictionary<string, object>());
                if (dictionaries.Any(d => d.Values.Any(t => !AzDataTableService.SupportedTypeList.Contains(t.GetType().Name.ToLower()))))
                {
                    string warningMessage = $@"An input entity has a field with a potentially unsupported type, please ensure that the input entities have only supported data types: https://docs.microsoft.com/en-us/rest/api/storageservices/understanding-the-table-service-data-model#property-types

Example of first entity provided
--------------------------------
{string.Join("\n", dictionaries.First().Select(d => string.Join("\n", $"[{d.Value.GetType().FullName}] {d.Key}")))}
";
                    WriteWarning(warningMessage);
                }
            }
            
            switch (ParameterSetName)
            {
                case "ConnectionString":
                    AzDataTableService.Connect(ConnectionString, TableName);
                    break;
                case "SAS":
                    AzDataTableService.Connect(SharedAccessSignature, TableName);
                    break;
                case "Key":
                    AzDataTableService.Connect(StorageAccountName, TableName, StorageAccountKey);
                    break;
                default:
                    throw new ArgumentException($"Unknown parameter set '{ParameterSetName}' was used!");
            }
        }
    }
}
