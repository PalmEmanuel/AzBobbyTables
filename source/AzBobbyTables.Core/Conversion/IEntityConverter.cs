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
    /// Gets the type name this converter handles (for error messages).
    /// </summary>
    string TypeName { get; }
}
