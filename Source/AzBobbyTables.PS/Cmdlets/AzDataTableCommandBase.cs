using PipeHow.AzBobbyTables.Core;
using System;
using System.Collections;
using System.Linq;
using System.Management.Automation;
using System.Threading;

namespace PipeHow.AzBobbyTables.Cmdlets
{
    /// <summary>
    /// The base class of the Azure Table commands, containing connection parameters.
    /// </summary>
    public class AzDataTableCommandBase : PSCmdlet
    {
        /// <summary>
        /// <para type="description">The name of the table.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "ConnectionString", Position = 0)]
        [Parameter(Mandatory = true, ParameterSetName = "SAS", Position = 0)]
        [Parameter(Mandatory = true, ParameterSetName = "Key", Position = 0)]
        [Parameter(Mandatory = true, ParameterSetName = "Token", Position = 0)]
        [Parameter(Mandatory = true, ParameterSetName = "ManagedIdentity", Position = 0)]
        [ValidateNotNullOrEmpty()]
        public string TableName { get; set; }

        /// <summary>
        /// <para type="description">If the table should be created if it does not exist.</para>
        /// </summary>
        [Parameter(ParameterSetName = "ConnectionString")]
        [Parameter(ParameterSetName = "SAS")]
        [Parameter(ParameterSetName = "Key")]
        [Parameter(ParameterSetName = "Token")]
        [Parameter(ParameterSetName = "ManagedIdentity")]
        public SwitchParameter CreateTableIfNotExists { get; set; }

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
        [Parameter(Mandatory = true, ParameterSetName = "Token")]
        [Parameter(Mandatory = true, ParameterSetName = "ManagedIdentity")]
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
        /// <para type="description">The token to use for authorization.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "Token")]
        [ValidateNotNullOrEmpty()]
        public string Token { get; set; }

        /// <summary>
        /// <para type="description">Specifies that the command should be run by a managed identity.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "ManagedIdentity")]
        public SwitchParameter ManagedIdentity { get; set; }

        protected AzDataTableService tableService;

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// The process step of the pipeline.
        /// </summary>
        protected override void BeginProcessing()
        {
            WriteDebug("ParameterSetName: " + ParameterSetName);
            
            // If the user specified the -Entity parameter, validate the data types of input
            if (MyInvocation.BoundParameters.ContainsKey("Entity"))
            {
                // Only writes a warning to the user if it doesn't
                ValidateEntitiesAndWarn();
            }
            
            switch (ParameterSetName)
            {
                case "ConnectionString":
                    tableService = AzDataTableService.CreateWithConnectionString(ConnectionString, TableName, CreateTableIfNotExists.IsPresent, cancellationTokenSource.Token);
                    break;
                case "SAS":
                    tableService = AzDataTableService.CreateWithSAS(SharedAccessSignature, TableName, CreateTableIfNotExists.IsPresent, cancellationTokenSource.Token);
                    break;
                case "Key":
                    tableService = AzDataTableService.CreateWithStorageKey(StorageAccountName, TableName, StorageAccountKey, CreateTableIfNotExists.IsPresent, cancellationTokenSource.Token);
                    break;
                case "Token":
                    tableService = AzDataTableService.CreateWithToken(StorageAccountName, TableName, Token, CreateTableIfNotExists.IsPresent, cancellationTokenSource.Token);
                    break;
                case "ManagedIdentity":
                    tableService = AzDataTableService.CreateWithToken(StorageAccountName, TableName, Helpers.GetManagedIdentityToken(StorageAccountName), CreateTableIfNotExists.IsPresent, cancellationTokenSource.Token);
                    break;
                default:
                    throw new ArgumentException($"Unknown parameter set '{ParameterSetName}' was used!");
            }
        }

        protected override void StopProcessing()
        {
            // Cancel any operations if user presses CTRL + C
            cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Validate the data types of user input to ensure it matches the supported table data types.
        /// Also warn if any value is null.
        /// </summary>
        private void ValidateEntitiesAndWarn()
        {
            Hashtable[] entities = MyInvocation.BoundParameters["Entity"] as Hashtable[];

            try
            {
                // Use OfType to get an enumerable of the keys, to select all values
                var values = entities.SelectMany(h => h.Keys.OfType<string>().Select(k => h[k]));
                // ValidateEntitiesAndWarn if any null values
                if (values.Any(v => v is null)) { WriteWarning("One of the provided entities has a null property value, which will not be included to the table operation."); }

                var firstEntity = entities.First();
                if (values.Any(v => v is not null && !AzDataTableService.SupportedTypeList.Contains(v.GetType().Name.ToLower())))
                {
                    string warningMessage = $@"An input entity has a field with a potentially unsupported type, please ensure that the input entities have only supported data types: https://docs.microsoft.com/en-us/rest/api/storageservices/understanding-the-table-service-data-model#property-types

Example of first entity provided
--------------------------------
{string.Join("\n", firstEntity.Keys.OfType<string>().Select(k => string.Join("\n", $"[{firstEntity[k].GetType().FullName}] {k}")))}
";
                    WriteWarning(warningMessage);
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "EntityTypeValidationFailed", ErrorCategory.InvalidData, entities));
            }
        }
    }
}
