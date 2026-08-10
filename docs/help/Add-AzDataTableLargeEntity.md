---
external help file: AzBobbyTables.PS.dll-Help.xml
Module Name: AzBobbyTables
online version:
schema: 2.0.0
---

# Add-AzDataTableLargeEntity

## SYNOPSIS

Add one or more entities to an Azure Table, transparently splitting entities that exceed the Azure Table Storage size limits.

## SYNTAX

### OperationType (Default)
```
Add-AzDataTableLargeEntity -Context <AzDataTableContext> -Entity <Object[]> [-OperationType <String>]
 [-CreateTableIfNotExists] [-MaxRetries <Int32>] [<CommonParameters>]
```

### Force
```
Add-AzDataTableLargeEntity -Context <AzDataTableContext> -Entity <Object[]> [-Force] [-CreateTableIfNotExists]
 [-MaxRetries <Int32>] [<CommonParameters>]
```

## DESCRIPTION

Add one or more entities to an Azure Table, as an array of either Hashtables, PSObjects, or SortedLists.

Unlike Add-AzDataTableEntity, this cmdlet accepts entities that exceed the Azure Table Storage size limits (64 KiB per string property, 1 MiB per entity) and splits them transparently:

- A string property larger than 32256 characters is stored as multiple chunk properties named `{Property}_Part0`, `{Property}_Part1`, and so on. A JSON manifest in the `SplitOverProps` property records which chunks belong to which original property.
- An entity that is still too large after property splitting is distributed over multiple rows. The first row keeps the original RowKey, additional rows are named `{RowKey}-part1`, `{RowKey}-part2`, and so on. Each row carries an `OriginalEntityId` property with the original RowKey, a `PartIndex` property with the row order, and a `PartCount` property with the total number of rows for the entity.

Use Get-AzDataTableLargeEntity to read the entities back in their original shape, and Remove-AzDataTableLargeEntity to delete them including all part rows.

If the same PartitionKey/RowKey combination appears more than once in a single call, only the last occurrence is written. This is required because Azure Table Storage rejects batch transactions that contain duplicate keys. When an entity is split across multiple rows, any part rows left from a previous larger version of that entity are deleted automatically after the new rows are written.

Note that when an entity is split over multiple rows, its rows are written as full replacements even with an OperationType of UpsertMerge, since the distribution of properties over rows changes between writes and merging would leave stale values behind. Entities small enough to fit in one row are written with the requested operation type. When the sizes of entities vary between writes across the splitting threshold, prefer UpsertReplace (or Force).

## EXAMPLES

### Example 1

```powershell
PS C:\> $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
PS C:\> $Report = @{ PartitionKey = 'Reports'; RowKey = 'tenant1'; Data = $LargeJsonString }
PS C:\> Add-AzDataTableLargeEntity -Entity $Report -Context $Context -Force
```

Add a report entity whose Data property may exceed the 64 KiB property limit, overwriting any existing version.

### Example 2

```powershell
PS C:\> $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
PS C:\> $CacheRows | Add-AzDataTableLargeEntity -Context $Context -Force -CreateTableIfNotExists
```

Write a collection of cache rows of arbitrary size from the pipeline, creating the table if needed.

## PARAMETERS

### -Context

A context object created by New-AzDataTableContext, with authentication information for the table to operate on.

```yaml
Type: AzDataTableContext
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -CreateTableIfNotExists

If the table should be created if it does not exist.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Entity

The entities to add to the table.

```yaml
Type: Object[]
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Force

Overwrites provided entities if they exist.

```yaml
Type: SwitchParameter
Parameter Sets: Force
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxRetries
The number of times to retry the operation when the request is throttled by the service with an HTTP 429 response. Between attempts the module waits for the duration indicated by the service's Retry-After response. Defaults to 0, which disables retries.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -OperationType

The operation type to perform on the entities. See the Azure SDK documentation for more information:

https://learn.microsoft.com/en-us/dotnet/api/azure.data.tables.tabletransactionactiontype

Entities split over multiple rows are always written as full replacements, see the description.

```yaml
Type: String
Parameter Sets: OperationType
Aliases:
Accepted values: Add, UpsertReplace, UpsertMerge

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable, -ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Collections.Hashtable[] or System.Management.Automation.PSObject[] or System.Collections.SortedList[]

This cmdlet takes either an array of hashtables, psobjects, or sorted lists as input to the Entity parameter, which can also be provided through the pipeline.

## OUTPUTS

### None

## NOTES

Only string properties are split. A non-string property that individually exceeds the service limits, such as a byte array over 64 KiB, is rejected by the service.

The storage format reserves some naming patterns in tables used with the large-entity cmdlets: property names ending in `_Part{n}` of another property, the property names `SplitOverProps`, `OriginalEntityId`, `PartIndex` and `PartCount`, and RowKeys of the form `{OtherRowKey}-part{n}`. Entities using such names can collide with the split representation of other entities.

## RELATED LINKS

[Get-AzDataTableLargeEntity](Get-AzDataTableLargeEntity.md)
[Remove-AzDataTableLargeEntity](Remove-AzDataTableLargeEntity.md)
