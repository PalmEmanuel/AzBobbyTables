---
external help file: AzBobbyTables.PS.dll-Help.xml
Module Name: AzBobbyTables
online version:
schema: 2.0.0
---

# Remove-AzDataTableLargeEntity

## SYNOPSIS

Remove one or more entities from an Azure Table, including any part rows they were split into by `Add-AzDataTableLargeEntity`.

## SYNTAX

```
Remove-AzDataTableLargeEntity -Context <AzDataTableContext> -Entity <Object[]> [-Force] [-MaxRetries <Int32>]
 [<CommonParameters>]
```

## DESCRIPTION

Remove one or more entities from an Azure Table, based on PartitionKey and RowKey.

In addition to the entity's own row, any part rows that the entity was split into when added by `Add-AzDataTableLargeEntity` (rows whose `OriginalEntityId` matches the entity's RowKey) are found and removed as well, so no orphaned parts are left behind. Part rows are removed without ETag validation; ETag validation, when not skipped with the `Force` parameter, applies only to the entity's own row.

## EXAMPLES

### Example 1

```powershell
PS C:\> $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
PS C:\> $Entity = Get-AzDataTableLargeEntity -Context $Context -Filter "RowKey eq 'tenant1'"
PS C:\> Remove-AzDataTableLargeEntity -Entity $Entity -Context $Context -Force
```

Remove an entity and all rows it was split into when added by `Add-AzDataTableLargeEntity`.

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

The entities to remove from the table.

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

Skips ETag validation and removes entity even if it has changed.

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
