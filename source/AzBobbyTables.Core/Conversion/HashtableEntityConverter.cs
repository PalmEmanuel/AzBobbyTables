using Azure.Data.Tables;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PipeHow.AzBobbyTables.Core.Conversion;

/// <summary>
/// Converter for Hashtable objects.
/// </summary>
public class HashtableEntityConverter : IEntityConverter
{
    public string TypeName => "Hashtable";

    public bool CanConvert(object obj) => obj is Hashtable;

    public bool ValidateEntity(object obj)
    {
        if (obj is not Hashtable hashtable)
            return false;

        var hasPartitionKey = hashtable.Keys.Cast<string>()
            .Any(key => key.Equals("PartitionKey", StringComparison.Ordinal));
        var hasRowKey = hashtable.Keys.Cast<string>()
            .Any(key => key.Equals("RowKey", StringComparison.Ordinal));

        return hasPartitionKey && hasRowKey;
    }

    public bool ValidateEntityPropertyValuesNotNull(object obj, out IEnumerable<string>? nullProperties)
    {
        if (obj is not Hashtable hashtable)
        {
            throw new ArgumentException($"Object is not a Hashtable, its type is '{obj?.GetType().FullName}'");
        }

        nullProperties = null;

        // Find any properties with null values
        nullProperties = hashtable.Keys.Cast<string>()
            .Where(key => hashtable[key] is null);
        
        return !nullProperties.Any();
    }

    public bool ValidateEntityPropertyTypes(object obj, out IEnumerable<string>? unsupportedProperties)
    {
        if (obj is not Hashtable hashtable)
        {
            throw new ArgumentException($"Object is not a Hashtable, its type is '{obj?.GetType().FullName}'");
        }

        unsupportedProperties = null;

        // Ensure that all keys are strings
        if (hashtable.Keys.Cast<object>().Any(k => k is not string))
        {
            throw new ArgumentException("All keys in the input Hashtable must be strings.");
        }

        // Check the values for unsupported types, save the keys of unsupported properties
            unsupportedProperties = hashtable.Keys.Cast<string>()
            .Where(k => !AzDataTableService.SupportedTypeList.Contains(hashtable[k].GetType().Name.ToLower()));

        return !unsupportedProperties.Any();
    }

    public TableEntity ConvertToTableEntity(object obj)
    {
        if (obj is not Hashtable hashtable)
            throw new ArgumentException($"Object is not a Hashtable, its type is '{obj?.GetType().FullName}'");

        var entity = new TableEntity();

        foreach (string key in hashtable.Keys)
        {
            switch (key)
            {
                case "ETag":
                    entity.ETag = new((string)hashtable[key]);
                    break;
                case "Timestamp":
                    entity.Timestamp = (DateTimeOffset?)hashtable[key];
                    break;
                default:
                    entity.Add(key, hashtable[key]);
                    break;
            }
        }

        return entity;
    }
}
