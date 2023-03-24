using System;
using System.Collections;
using System.Linq;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Validation;

/// <summary>
/// Validates that the first entity (as a model for all entities provided) has PartitionKey and RowKey.
/// </summary>
class ValidateEntityAttribute : ValidateArgumentsAttribute
{
    protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
    {
        // The input is an object array, but it must hold either Hashtables or PSObjects
        var firstEntity = ((object[])arguments).First();
        // Validate the first entity in the list
        // First on type, then ensure correct values
        switch (firstEntity)
        {
            case Hashtable hashtable:
                if (!hashtable.ContainsKey("PartitionKey"))
                {
                    throw new ArgumentException("PartitionKey must be provided in the input hashtable entity in the correct casing!");
                }
                if (!hashtable.ContainsKey("RowKey"))
                {
                    throw new ArgumentException("RowKey must be provided in the input hashtable entity in the correct casing!");
                }
                break;
            case PSObject psobject:
                if (!psobject.Properties.Any(p => p.Name == "PartitionKey"))
                {
                    throw new ArgumentException("PartitionKey must be provided in the input psobject entity in the correct casing!");
                }
                if (!psobject.Properties.Any(p => p.Name == "RowKey"))
                    {
                    throw new ArgumentException("RowKey must be provided in the input psobject entity in the correct casing!");
                }
                break;
            default:
                throw new ArgumentException($"Input entities have to be either Hashtable or PSObject, first entity was {firstEntity.GetType().FullName}!");
        }
    }
}
