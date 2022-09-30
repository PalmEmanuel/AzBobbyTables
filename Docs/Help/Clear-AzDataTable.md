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

### ConnectionString
```
Clear-AzDataTable [-TableName] <String> [-CreateTableIfNotExists] -ConnectionString <String>
 [<CommonParameters>]
```

### SAS
```
Clear-AzDataTable [-TableName] <String> [-CreateTableIfNotExists] -SharedAccessSignature <Uri>
 [<CommonParameters>]
```

### Key
```
Clear-AzDataTable [-TableName] <String> [-CreateTableIfNotExists] -StorageAccountName <String>
 -StorageAccountKey <String> [<CommonParameters>]
```

### Token
```
Clear-AzDataTable [-TableName] <String> [-CreateTableIfNotExists] -StorageAccountName <String> -Token <String>
 [<CommonParameters>]
```

## DESCRIPTION
Clear all entities from an Azure Table.

## EXAMPLES

### Example 1
```powershell
PS C:\> Clear-AzDataTable -TableName $TableName -ConnectionString $ConnectionString
```

Clear all entities from a table using a connection string.

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

### -SharedAccessSignature
The table service SAS URL.
The table endpoint of the storage account, with the shared access token appended to it.

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
Parameter Sets: Key, Token
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
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Token
{{ Fill Token Description }}

```yaml
Type: String
Parameter Sets: Token
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None

## OUTPUTS

### None

## NOTES

## RELATED LINKS
