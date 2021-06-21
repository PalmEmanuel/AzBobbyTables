using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Validation
{
    class ValidateTableEntries : ValidateArgumentsAttribute
    {
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            var rows = (IEnumerable<Hashtable>)arguments;

            foreach (var row in rows)
            {
                if (!row.ContainsKey("PartitionKey")) { throw new ArgumentException("PartitionKey must be provided in the hashtable in the right casing!"); }
                if (!row.ContainsKey("RowKey")) { throw new ArgumentException("RowKey must be provided in the hashtable in the right casing!"); }
            }
        }
    }
}
