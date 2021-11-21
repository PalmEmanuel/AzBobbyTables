using PipeHow.AzBobbyTables.Validation;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets
{
    /// <summary>
    /// <para type="synopsis">Add one or more entities to an Azure Table.</para>
    /// <para type="description">Add an entity to an Azure Table, as a PSCustomObject.</para>
    /// <example>
    ///     <code>$User = [pscustomobject]@{ FirstName = 'Bobby'; LastName = 'Tables' }</code>
    ///     <code>Add-AzDataTableEntity -Entity $User -ConnectionString $ConnectionString</code>
    ///     <para>Add the user "Bobby Tables" to the table using a connection string.</para>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "AzDataTableEntity")]
    [Alias("Add-AzDataTableRow")]
    public class AddAzDataTableEntity : AzDataTableEntityCommandBase
    {
        /// <summary>
        /// <para type="description">The entities to add to the table.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "ConnectionString", ValueFromPipeline = true)]
        [Parameter(Mandatory = true, ParameterSetName = "SAS", ValueFromPipeline = true)]
        [Parameter(Mandatory = true, ParameterSetName = "Key", ValueFromPipeline = true)]
        [Alias("Row", "Entry", "Property")]
        [ValidateEntity]
        public PSObject[] Entity { get; set; }

        /// <summary>
        /// <para type="description">Overwrites provided entities if they exist.</para>
        /// </summary>
        [Parameter(ParameterSetName = "ConnectionString")]
        [Parameter(ParameterSetName = "SAS")]
        [Parameter(ParameterSetName = "Key")]
        [Alias("UpdateExisting")]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// The process step of the pipeline.
        /// </summary>
        protected override void ProcessRecord()
        {
            AzDataTableService.AddEntitiesToTable(Entity, Force.IsPresent);
        }
    }
}
