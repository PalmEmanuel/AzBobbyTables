---
external help file: AzBobbyTables.PS.dll-Help.xml
Module Name: AzBobbyTables
online version:
schema: 2.0.0
---

# Update-AzDataTableLargeEntity

## SYNOPSIS

Update one or more entities that already exist in an Azure Table, transparently handling entities that were split across multiple properties or rows by `Add-AzDataTableLargeEntity`.

## SYNTAX

```
Update-AzDataTableLargeEntity -Context <AzDataTableContext> -Entity <Object[]> [-OperationType <String>]
 [-Force] [-MaxRetries <Int32>] [<CommonParameters>]
```

## DESCRIPTION

Update one or more entities that already exist in an Azure Table, based on PartitionKey and RowKey. Entities that do not exist cause an error and are never created, unlike the upsert operation types of `Add-AzDataTableLargeEntity`.

`UpdateReplace` replaces the whole logical entity: the entity's own row is updated (and fails if missing), any additional part rows the new version needs are upserted, and part rows the new version no longer uses are removed.

`UpdateMerge` merges the given properties into the logical entity. A plain single-row entity is merged in place. An entity that was split for size, or given properties that are themselves oversized, are read, merged in memory and rewritten, since merging onto the physical rows directly would corrupt the split-entity format.

ETag validation, when not skipped with the `Force` parameter, applies to the entity's own row. The read-merge-rewrite path is not atomic.

## EXAMPLES

### Example 1

```powershell
PS C:\> $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
PS C:\> Update-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'tenant1'; RowKey = 'record1'; Processed = $true }
```

Merge a property onto an existing entity. If the entity does not exist, the update fails instead of creating it.

### Example 2

```powershell
PS C:\> $Entity = Get-AzDataTableLargeEntity -Context $Context -Filter "RowKey eq 'record1'"
PS C:\> $Entity.Data = $NewLargeValue
PS C:\> Update-AzDataTableLargeEntity -Context $Context -Entity $Entity -OperationType UpdateReplace
```

Replace an existing entity with a new version, splitting it over properties and rows as needed and removing part rows the new version no longer uses. The ETag from the read is validated, so a concurrent change fails the update.

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

### -Entity

The entities to update in the table.

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

Skips ETag validation and updates entity even if it has changed.

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

The type of operation to perform on the entities, either UpdateMerge or UpdateReplace. Defaults to UpdateMerge.

```yaml
Type: String
Parameter Sets: (All)
Aliases:
Accepted values: UpdateMerge, UpdateReplace

Required: False
Position: Named
Default value: UpdateMerge
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

## RELATED LINKS

[Add-AzDataTableLargeEntity](Add-AzDataTableLargeEntity.md)
[Get-AzDataTableLargeEntity](Get-AzDataTableLargeEntity.md)
[Remove-AzDataTableLargeEntity](Remove-AzDataTableLargeEntity.md)
