---
external help file: AzBobbyTables.PS.dll-Help.xml
Module Name: AzBobbyTables
online version:
schema: 2.0.0
---

# Get-AzDataTable

## SYNOPSIS

Get the names of all tables in the storage account.

## SYNTAX

```
Get-AzDataTable -Context <AzDataTableContext> [-Filter <String>]
 [<CommonParameters>]
```

## DESCRIPTION

Get the names of all tables in the storage account.

The optional `-Filter` parameter can be used to filter the tables returned. For more information on the filter syntax, see the Azure Table service documentation:

https://learn.microsoft.com/en-us/rest/api/storageservices/Querying-Tables-and-Entities

## EXAMPLES

### Example 1

```powershell
PS C:\> Get-AzDataTable -Context $Context
```

Gets all table names in the storage account.

### Example 2

```powershell
PS C:\> Get-AzDataTable -Context $Context -Filter "TableName eq '$MyTableName'"
```

Gets the table named `$MyTableName` to see if it exists.

## PARAMETERS

### -Context

A context object created by New-AzDataTableContext, with authentication information for the storage account to operate on.

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

### -Filter

A string to filter the tables returned. For more information on the filter syntax, see the Azure Table service documentation:

https://learn.microsoft.com/en-us/rest/api/storageservices/Querying-Tables-and-Entities

```yaml
Type: String
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

### None

## OUTPUTS

### System.String

## NOTES

## RELATED LINKS
