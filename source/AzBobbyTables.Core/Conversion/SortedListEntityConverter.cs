using Azure.Data.Tables;
using System;
using System.Collections;
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

        var hasPartitionKey = sortedList.Keys.Cast<string>()
            .Any(key => key.Equals("PartitionKey", StringComparison.Ordinal));
        var hasRowKey = sortedList.Keys.Cast<string>()
            .Any(key => key.Equals("RowKey", StringComparison.Ordinal));

        return hasPartitionKey && hasRowKey;
    }

    public TableEntity ConvertToTableEntity(object obj)
    {
        if (obj is not SortedList sortedList)
            throw new ArgumentException($"Object is not a SortedList: {obj?.GetType().FullName}");

        var entity = new TableEntity();
        
        foreach (string key in sortedList.Keys.Cast<string>())
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
}
