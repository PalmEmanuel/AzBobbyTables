using System;
using System.Collections.Generic;
using System.Linq;

namespace PipeHow.AzBobbyTables.Core.Conversion;

/// <summary>
/// Registry for managing entity converters.
/// </summary>
public class EntityConverterRegistry
{
    private static readonly Lazy<EntityConverterRegistry> _instance = new(() => new EntityConverterRegistry());
    private readonly List<IEntityConverter> _converters = new();

    /// <summary>
    /// Gets the singleton instance of the registry.
    /// </summary>
    public static EntityConverterRegistry Instance => _instance.Value;

    private EntityConverterRegistry()
    {
        // Register built-in converters
        _converters.Add(new HashtableEntityConverter());
        _converters.Add(new PSObjectEntityConverter());
        _converters.Add(new SortedListEntityConverter());
    }

    /// <summary>
    /// Gets a converter that can handle the specified object.
    /// </summary>
    /// <param name="obj">The object to find a converter for.</param>
    /// <returns>A converter that can handle the object, or null if none found.</returns>
    public IEntityConverter GetConverter(object obj)
    {
        return _converters.FirstOrDefault(c => c.CanConvert(obj));
    }

    /// <summary>
    /// Validates that an object can be converted and has required properties.
    /// </summary>
    /// <param name="obj">The object to validate.</param>
    /// <returns>True if the object is valid.</returns>
    public bool ValidateEntity(object obj)
    {
        var converter = GetConverter(obj);
        return converter?.ValidateEntity(obj) ?? false;
    }

    /// <summary>
    /// Gets all registered converter type names.
    /// </summary>
    /// <returns>A list of supported type names.</returns>
    public IEnumerable<string> GetSupportedTypeNames()
    {
        return _converters.Select(c => c.TypeName);
    }
}
