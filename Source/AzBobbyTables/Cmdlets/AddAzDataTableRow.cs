using PipeHow.AzBobbyTables.Validation;
using System.Collections;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets
{
    /// <summary>
    /// Add a row to an Azure Table.
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "AzDataTableRow")]
    public class AddAzDataTableRow : Cmdlet
    {
        /// <summary>
        /// <para type="description">The connection string to the table.</para>
        /// </summary>
        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty()]
        public string ConnectionString { get; set; }

        /// <summary>
        /// <para type="description">The name of the table.</para>
        /// </summary>
        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty()]
        public string TableName { get; set; }

        /// <summary>
        /// <para type="description">The entries to add to the table.</para>
        /// </summary>
        [Parameter(Mandatory = true)]
        [ValidateTableEntries()]
        public Hashtable[] Rows { get; set; }

        protected override void BeginProcessing()
        {
            AzDataTableService.Connect(ConnectionString, TableName);
        }

        protected override void ProcessRecord()
        {
            AzDataTableService.AddRowsToTable(Rows);
        }
    }
}
