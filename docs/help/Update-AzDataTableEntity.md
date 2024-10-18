---
external help file: AzBobbyTables.PS.dll-Help.xml
Module Name: AzBobbyTables
online version:
schema: 2.0.0
---

# Update-AzDataTableEntity

## SYNOPSIS

Update one or more entities in an Azure Table.

## SYNTAX

```
Update-AzDataTableEntity -Context <AzDataTableContext> -Entity <Object[]> [-Force]
 [<CommonParameters>]
```

## DESCRIPTION

Update one or more entities already existing in an Azure Table, as an array of either Hashtables or PSObjects.

For adding and overwriting, also see the command Add-AzDataTableEntity.

The PartitionKey and RowKey cannot be updated.

## EXAMPLES

### Example 1

```powershell
PS C:\> $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
PS C:\> $UserEntity = Get-AzDataTableEntity -Filter "FirstName eq 'Bobby'" -Context $Context
PS C:\> $UserEntity['LastName'] = 'Tables'
PS C:\> Update-AzDataTableEntity -Entity $UserEntity -Context $Context
```

Update the last name of the user "Bobby" to "Tables" using a connection string.

### Example 2

```powershell
PS C:\> $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
PS C:\> $UserEntity = Get-AzDataTableEntity -Filter "FirstName eq 'Bobby'" -Context $Context
PS C:\> $UserEntity['LastName'] = 'Tables'
PS C:\> # Imagine that the user is updated somewhere else
PS C:\> Update-AzDataTableEntity -Entity $UserEntity -Context $Context
PS C:\> # ERROR - The ETag of UserEntity does not match
PS C:\> Update-AzDataTableEntity -Entity $UserEntity -Context $Context -Force
PS C:\> # OK - The -Force switch overrides ETag validation
```

Force update the last name of the user "Bobby" to "Tables" using a connection string, overriding ETag validation.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable, -ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Collections.Hashtable[] or System.Management.Automation.PSObject[]

This cmdlet takes either an array of hashtables or psobjects as input to the Entity parameter, which can also be provided through the pipeline.

## OUTPUTS

### None

## NOTES

## RELATED LINKS
