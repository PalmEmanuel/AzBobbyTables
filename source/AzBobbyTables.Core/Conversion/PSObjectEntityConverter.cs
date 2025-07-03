using Azure.Data.Tables;
using System;
using System.Linq;
using System.Management.Automation;

namespace PipeHow.AzBobbyTables.Core.Conversion;

/// <summary>
/// Converter for PSObject objects.
/// </summary>
public class PSObjectEntityConverter : IEntityConverter
{
    public string TypeName => "PSObject";

    public bool CanConvert(object obj) => obj is PSObject;

    public bool ValidateEntity(object obj)
    {
        if (obj is not PSObject psobject)
            return false;

        var hasPartitionKey = psobject.Properties
            .Any(p => p.Name.Equals("PartitionKey", StringComparison.Ordinal));
        var hasRowKey = psobject.Properties
            .Any(p => p.Name.Equals("RowKey", StringComparison.Ordinal));

        return hasPartitionKey && hasRowKey;
    }

    public TableEntity ConvertToTableEntity(object obj)
    {
        if (obj is not PSObject psobject)
            throw new ArgumentException($"Object is not a PSObject: {obj?.GetType().FullName}");

        var entity = new TableEntity();
        
        foreach (var prop in psobject.Properties)
        {
            switch (prop.Name)
            {
                case "ETag":
                    entity.ETag = new((string)prop.Value);
                    break;
                case "Timestamp":
                    entity.Timestamp = (DateTimeOffset?)prop.Value;
                    break;
                default:
                    entity.Add(prop.Name, prop.Value);
                    break;
            }
        }

        return entity;
    }
}
