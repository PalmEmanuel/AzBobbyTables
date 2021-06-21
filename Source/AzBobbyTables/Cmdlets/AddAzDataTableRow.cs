using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Cmdlets
{
    /// <summary>
    /// 
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "AzDataTableRow")]
    public class AddAzDataTableRow : Cmdlet
    {
        /// <summary>
        /// <para type="description">The name of the table.</para>
        /// </summary>
        [Parameter(Mandatory = true)]
        public string ConnectionS { get; set; }

        /// <summary>
        /// <para type="description">The name of the table.</para>
        /// </summary>
        [Parameter(Mandatory = true)]
        public string TableName { get; set; }

        /// <summary>
        /// <para type="description">The entries to add to the table.</para>
        /// </summary>
        [Parameter(Mandatory = true)]
        public Hashtable Row { get; set; }

        public AddAzDataTableRow()
        {
            base.ProcessRecord();

            AzDataTableService.Connect(ConnectionS, TableName);

            AzDataTableService.AddRowToTable(Row);
        }
    }
}
