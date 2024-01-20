using System;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets;

/// <summary>
/// Create a new context for working with a data table.
/// </summary>
[Cmdlet(VerbsCommon.New, "AzDataTableContext")]
public class NewAzDataTableContext : AzDataTableCommand // Inherit only base behavior, no parameters
{
    /// <summary>
    /// <para type="description">The name of the table.</para>
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "ConnectionString")]
    [Parameter(Mandatory = true, ParameterSetName = "SAS")]
    [Parameter(Mandatory = true, ParameterSetName = "Key")]
    [Parameter(Mandatory = true, ParameterSetName = "Token")]
    [Parameter(Mandatory = true, ParameterSetName = "ManagedIdentity")]
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
    [ValidatePattern("https?://.*")]
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

    /// <summary>
    /// <para type="description">Specifices the client id of a user-assigned managed identity.</para>
    /// </summary>
    [Parameter(ParameterSetName = "ManagedIdentity")]
    public string ClientId { get; set; }

    /// <summary>
    /// The end step of the pipeline.
    /// </summary>
    protected override void EndProcessing()
    {
        // Try parsing ParameterSetName to enum AzDataTableConnectionType, write error if it fails
        if (!Enum.TryParse(ParameterSetName, out AzDataTableConnectionType connectionType))
            WriteError(new ErrorRecord(new ArgumentException("Incorrect connection type!"), "ConnectionTypeError", ErrorCategory.InvalidType, connectionType));

        // Output the AzDataTableContext to user for further operations
        WriteObject(new AzDataTableContext(TableName, connectionType, ConnectionString, StorageAccountName, StorageAccountKey, SharedAccessSignature, ClientId, Token));

        base.EndProcessing();
    }
}
