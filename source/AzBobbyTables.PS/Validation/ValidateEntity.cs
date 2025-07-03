using System;
using System.Linq;
using System.Management.Automation;
using PipeHow.AzBobbyTables.Core.Conversion;

namespace PipeHow.AzBobbyTables.Validation;

/// <summary>
/// Validates that the first entity (as a model for all entities provided) has PartitionKey and RowKey.
/// </summary>
class ValidateEntityAttribute : ValidateArgumentsAttribute
{
    protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
    {
        // The input is an object array
        var entities = (object[])arguments;
        if (entities.Length == 0)
        {
            throw new ArgumentException("At least one entity must be provided!");
        }

        var firstEntity = entities.First();
        var registry = EntityConverterRegistry.Instance;

        // Check if we have a converter for this type
        var converter = registry.GetConverter(firstEntity);
        if (converter == null)
        {
            var supportedTypes = string.Join(", ", registry.GetSupportedTypeNames());
            throw new ArgumentException($"Unsupported entity type '{firstEntity.GetType().FullName}'. Supported types are: {supportedTypes}");
        }

        // Validate that the entity has required properties
        if (!converter.ValidateEntity(firstEntity))
        {
            throw new ArgumentException($"PartitionKey and RowKey must be provided in the input {converter.TypeName} entity with correct casing!");
        }
    }
}
