---
external help file: AzBobbyTables.PS.dll-Help.xml
Module Name: AzBobbyTables
online version:
schema: 2.0.0
---

# Get-AzDataTableLargeEntity

## SYNOPSIS

Get one or more entities from an Azure Table, reassembling entities that were split by Add-AzDataTableLargeEntity.

## SYNTAX

### TableOperation (Default)
```
Get-AzDataTableLargeEntity -Context <AzDataTableContext> [-Filter <String>] [-Property <String[]>]
 [-First <Int32>] [-Skip <Int32>] [-Sort <String[]>] [-MaxRetries <Int32>] [<CommonParameters>]
```

### Count
```
Get-AzDataTableLargeEntity -Context <AzDataTableContext> [-Count] [-MaxRetries <Int32>] [<CommonParameters>]
```

## DESCRIPTION

Get one or more entities from an Azure Table, reassembling entities that were split across multiple properties or rows because they exceeded the Azure Table Storage size limits.

Rows belonging to one logical entity are recognized by their `OriginalEntityId` property and merged in `PartIndex` order, after which chunked properties recorded in the `SplitOverProps` manifest are joined back into their original single property. Entities that were never split are returned as-is.

If both a plain row and leftover part rows exist for the same RowKey, the plain row wins, so an entity that was rewritten smaller after having been split still reads correctly.

Since split entities span multiple physical rows, use filters that do not separate an entity from its parts; filtering on the PartitionKey level is safe. First, Skip, Sort and Count operate on physical rows, before reassembly. A Property selection that excludes the split markers (OriginalEntityId, PartIndex, SplitOverProps and the chunk properties) prevents reassembly.

## EXAMPLES

### Example 1

```powershell
PS C:\> $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
PS C:\> Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'Reports'"
```

Get all report entities, transparently reassembling any that were split on write.

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

### -Count

Specify that the output should only specify the number of entities. Counts physical rows, so each part row of a split entity counts individually.

```yaml
Type: SwitchParameter
Parameter Sets: Count
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Filter

The OData filter to use in the query.

```yaml
Type: String
Parameter Sets: TableOperation
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -First

The amount of physical rows to retrieve, counted before split entities are reassembled.

```yaml
Type: Int32
Parameter Sets: TableOperation
Aliases: Top, Take

Required: False
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

### -Property

The properties to return for the entities. Selecting properties that exclude the split markers prevents reassembly of split entities.

```yaml
Type: String[]
Parameter Sets: TableOperation
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Skip

The amount of physical rows to skip from the query result, counted before split entities are reassembled.

```yaml
Type: Int32
Parameter Sets: TableOperation
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Sort

The names of one or more properties to sort by, in order. Sorting applies to physical rows before reassembly.

```yaml
Type: String[]
Parameter Sets: TableOperation
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable, -ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None

## OUTPUTS

### System.Management.Automation.PSObject

## NOTES

The ETag and Timestamp of a reassembled entity are taken from its first part row.

## RELATED LINKS

[Add-AzDataTableLargeEntity](Add-AzDataTableLargeEntity.md)
[Remove-AzDataTableLargeEntity](Remove-AzDataTableLargeEntity.md)
