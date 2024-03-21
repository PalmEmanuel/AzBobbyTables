---
external help file: AzBobbyTables.PS.dll-Help.xml
Module Name: AzBobbyTables
online version:
schema: 2.0.0
---

# Remove-AzDataTableEntity

## SYNOPSIS

Remove one or more entities from an Azure Table.

## SYNTAX

```
Remove-AzDataTableEntity -Context <AzDataTableContext> -Entity <Object[]> [-Force]
 [<CommonParameters>]
```

## DESCRIPTION

Remove one or more entities from an Azure Table, as an array of either Hashtables or PSObjects, based on PartitionKey and RowKey.

## EXAMPLES

### Example 1

```powershell
PS C:\> $Context = New-AzDataTableContext -TableName $TableName -StorageAccountName $Name -StorageAccountKey $Key
PS C:\> $Entity = @{ PartitionKey = 'Example'; RowKey = '1' }
PS C:\> Remove-AzDataTableEntity -Entity $Entity -TableName $TableName -Context $Context
```

Remove the entity with PartitionKey "Example" and RowKey "1", using the storage account name and an access key.

### Example 2

```powershell
PS C:\> $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
PS C:\> $UserEntity = Get-AzDataTableEntity -Filter "FirstName eq 'Bobby' and LastName eq 'Tables'" -Context $Context
PS C:\> Remove-AzDataTableEntity -Entity $UserEntity -Context $Context
```

Get the user "Bobby Tables" from the table using a connection string, then remove the user using the storage account name and an access key.

### Example 3

```powershell
PS C:\> $Context = New-AzDataTableContext -StorageAccountName $StorageName -TableName $TableName -ManagedIdentity
PS C:\> $Users = Get-AzDataTableEntity -Filter "LastName eq 'Tables'" -Context $Context
PS C:\> Remove-AzDataTableEntity -Entity $Users -Context $Context
```

Gets all users with the last name "Tables" from the table using a system-assigned managed identity, then removes the users.

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

Skips ETag validation and remove entity even if it has changed.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable, -ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Collections.Hashtable[] or System.Management.Automation.PSObject[]

This cmdlet takes either an array of hashtables or psobjects as input to the Entity parameter, which can also be provided through the pipeline.

## OUTPUTS

### None

## NOTES

## RELATED LINKS
