using Azure.Data.Tables;
using System;
using System.Collections;
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

    public TableEntity ConvertToTableEntity(object obj)
    {
        if (obj is not Hashtable hashtable)
            throw new ArgumentException($"Object is not a Hashtable: {obj?.GetType().FullName}");

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
