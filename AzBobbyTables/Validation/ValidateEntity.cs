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
            var entities = (IEnumerable<PSObject>)arguments;

            if (entities.Any(e => !e.Properties.Any(p => p.Name == "PartitionKey")))
            {
                throw new ArgumentException("PartitionKey must be provided in the input entity in the correct casing!");
            }
            if (entities.Any(e => !e.Properties.Any(p => p.Name == "RowKey")))
            {
                throw new ArgumentException("RowKey must be provided in the input entity in the correct casing!");
            }
        }
    }
}
