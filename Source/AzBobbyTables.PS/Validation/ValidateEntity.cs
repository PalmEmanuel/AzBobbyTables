using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Validation
{
    class ValidateEntity : ValidateArgumentsAttribute
    {
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            var entities = (IEnumerable<Hashtable>)arguments;

            if (entities.Any(e => !e.ContainsKey("PartitionKey")))
            {
                throw new ArgumentException("PartitionKey must be provided in the input entity in the correct casing!");
            }
            if (entities.Any(e => !e.ContainsKey("RowKey")))
            {
                throw new ArgumentException("RowKey must be provided in the input entity in the correct casing!");
            }
        }
    }
}
