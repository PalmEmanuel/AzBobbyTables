---
external help file: AzBobbyTables.PS.dll-Help.xml
Module Name: AzBobbyTables
online version:
schema: 2.0.0
---

# Get-AzDataTableEntity

## SYNOPSIS
Get one or more entities from an Azure Table.

## SYNTAX

### ConnectionString
```
Get-AzDataTableEntity [-Filter <String>] -TableName <String> [-CreateTableIfNotExists]
 -ConnectionString <String> [<CommonParameters>]
```

### SAS
```
Get-AzDataTableEntity [-Filter <String>] -TableName <String> [-CreateTableIfNotExists]
 -SharedAccessSignature <Uri> [<CommonParameters>]
```

### Key
```
Get-AzDataTableEntity [-Filter <String>] -TableName <String> [-CreateTableIfNotExists]
 -StorageAccountName <String> -StorageAccountKey <String> [<CommonParameters>]
```

## DESCRIPTION
Get either all entities from an Azure Table, or those matching a provided OData filter.

Documentation on querying tables and entities: https://docs.microsoft.com/en-gb/rest/api/storageservices/querying-tables-and-entities

## EXAMPLES

### Example 1
```powershell
PS C:\> $UserEntity = Get-AzDataTableEntity -Filter "FirstName eq 'Bobby' and LastName eq 'Tables'" -TableName $TableName -ConnectionString $ConnectionString
```

Get the user "Bobby Tables" from the table using a connection string.

## PARAMETERS

### -ConnectionString
The connection string to the storage account.

```yaml
Type: String
Parameter Sets: ConnectionString
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Filter
The OData filter to use in the query.
Documentation on querying tables and entities: https://docs.microsoft.com/en-gb/rest/api/storageservices/querying-tables-and-entities

```yaml
Type: String
Parameter Sets: (All)
Aliases: Query

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -SharedAccessSignature
The table service SAS URL.
The table endpoint of the storage account, with the shared access token token appended to it.

```yaml
Type: Uri
Parameter Sets: SAS
Aliases: SAS

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StorageAccountKey
The storage account access key.

```yaml
Type: String
Parameter Sets: Key
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StorageAccountName
The name of the storage account.

```yaml
Type: String
Parameter Sets: Key
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TableName
The name of the table.

```yaml
Type: String
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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String

## OUTPUTS

### System.Collections.Hashtable[]

## NOTES

## RELATED LINKS
