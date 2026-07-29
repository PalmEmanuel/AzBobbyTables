using Azure.Data.Tables;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PipeHow.AzBobbyTables.Core.Conversion;

/// <summary>
/// Converter for SortedList objects.
/// </summary>
public class SortedListEntityConverter : IEntityConverter
{
    public string TypeName => "SortedList";

    public bool CanConvert(object obj) => obj is SortedList;

    public bool ValidateEntity(object obj)
    {
        if (obj is not SortedList sortedList)
            return false;

        // Single pass over the keys. Cast to string rather than pattern matching so a non-string
        // key still throws, as Cast<string>() did.
        var hasPartitionKey = false;
        var hasRowKey = false;

        foreach (string key in sortedList.Keys)
        {
            if (!hasPartitionKey && key.Equals("PartitionKey", StringComparison.Ordinal))
            {
                hasPartitionKey = true;
            }
            else if (!hasRowKey && key.Equals("RowKey", StringComparison.Ordinal))
            {
                hasRowKey = true;
            }

            if (hasPartitionKey && hasRowKey)
            {
                return true;
            }
        }

        return false;
    }

    public TableEntity ConvertToTableEntity(object obj)
    {
        if (obj is not SortedList sortedList)
            throw new ArgumentException($"Object is not a SortedList, its type is '{obj?.GetType().FullName}'");

        var entity = new TableEntity();
        
        // No Cast<string>() needed: the foreach already casts each key, and throws the same way
        // on a non-string key. Cast only added a second iterator per entity.
        foreach (string key in sortedList.Keys)
        {
            switch (key)
            {
                case "ETag":
                    entity.ETag = new((string)sortedList[key]);
                    break;
                case "Timestamp":
                    entity.Timestamp = (DateTimeOffset?)sortedList[key];
                    break;
                default:
                    entity.Add(key, sortedList[key]);
                    break;
            }
        }

        return entity;
    }

    public bool ValidateEntityPropertyTypes(object obj, out IEnumerable<string>? unsupportedProperties)
    {
        if (obj is not SortedList sortedList)
        {
            throw new ArgumentException($"Object is not a SortedList, its type is '{obj?.GetType().FullName}'");
        }

        unsupportedProperties = null;

        // Ensure that all keys are strings
        if (sortedList.Keys.Cast<object>().Any(k => k is not string))
        {
            throw new ArgumentException("All keys in the input SortedList must be strings.");
        }

        // Check for unsupported property types
        unsupportedProperties = sortedList.Keys.Cast<string>().Where(k => k is not null && !AzDataTableService.SupportedTypeList.Contains(sortedList[k].GetType().Name.ToLower()));

        return !unsupportedProperties.Any();
    }

    public bool ValidateEntityPropertyValuesNotNull(object obj, out IEnumerable<string>? nullProperties)
    {
        if (obj is not SortedList sortedList)
        {
            throw new ArgumentException($"Object is not a SortedList, its type is '{obj?.GetType().FullName}'");
        }

        nullProperties = null;

        // Find any properties with null values
        nullProperties = sortedList.Keys.Cast<string>()
            .Where(key => sortedList[key] is null);

        return !nullProperties.Any();
    }
}
