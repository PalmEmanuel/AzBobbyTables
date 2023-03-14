---
external help file: AzBobbyTables.PS.dll-Help.xml
Module Name: AzBobbyTables
online version:
schema: 2.0.0
---

# Clear-AzDataTable

## SYNOPSIS

Clear all entities from an Azure Table.

## SYNTAX

### TableOperation

```
Clear-AzDataTable -Context <AzDataTableContext> [-CreateTableIfNotExists] [<CommonParameters>]
```

### Count

```
Clear-AzDataTable -Context <AzDataTableContext> [-CreateTableIfNotExists] [<CommonParameters>]
```

## DESCRIPTION

Clear all entities from an Azure Table.

## EXAMPLES

### Example 1

```powershell
PS C:\> $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
PS C:\> Clear-AzDataTable -Context $Context
```

Clear all entities from a table using a connection string.

### Example 2

```powershell
PS C:\> $Context = New-AzDataTableContext -TableName $TableName -StorageAccountName $Name -ManagedIdentity
PS C:\> Clear-AzDataTable $Context
```

Clear all entities from a table using a managed identity.

## PARAMETERS

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

### -Context

A context object created by New-AzDataTableContext, with authentication information for the table to operate on.

```yaml
Type: AzDataTableContext
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

## OUTPUTS

## NOTES

## RELATED LINKS
