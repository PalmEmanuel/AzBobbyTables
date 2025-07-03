using System.Collections.Generic;
using Azure.Data.Tables;

namespace PipeHow.AzBobbyTables.Core.Conversion;

/// <summary>
/// Interface for converting objects to TableEntity instances.
/// </summary>
public interface IEntityConverter
{
    /// <summary>
    /// Determines if this converter can handle the specified object type.
    /// </summary>
    /// <param name="obj">The object to check.</param>
    /// <returns>True if this converter can handle the object type.</returns>
    bool CanConvert(object obj);

    /// <summary>
    /// Converts an object to a TableEntity.
    /// </summary>
    /// <param name="obj">The object to convert.</param>
    /// <returns>A TableEntity representation of the object.</returns>
    TableEntity ConvertToTableEntity(object obj);

    /// <summary>
    /// Validates that the object has the required PartitionKey and RowKey.
    /// </summary>
    /// <param name="obj">The object to validate.</param>
    /// <returns>True if the object is valid.</returns>
    bool ValidateEntity(object obj);

    /// <summary>
    /// Validates that the object does not contain unsupported property types.
    /// </summary>
    /// <param name="obj">The object to validate.</param>
    /// <param name="unsupportedProperties">Outputs the names of unsupported properties, if any.</param>
    /// <returns>True if there are only supported property types.</returns>
    bool ValidateEntityPropertyTypes(object obj, out IEnumerable<string>? unsupportedProperties);

    /// <summary>
    /// Validates that the object does not contain property values that are null.
    /// </summary>
    /// <param name="obj">The object to validate.</param>
    /// <param name="nullProperties">Outputs the names of properties with null values, if any.</param>
    /// <returns>True if there are no properties with null values.</returns>
    bool ValidateEntityPropertyValuesNotNull(object obj, out IEnumerable<string>? nullProperties);

    /// <summary>
    /// Gets the type name this converter handles (for error messages).
    /// </summary>
    string TypeName { get; }
}
