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

### ConnectionString
```
Update-AzDataTableEntity -Entity <Hashtable[]> -TableName <String> -ConnectionString <String>
 [<CommonParameters>]
```

### SAS
```
Update-AzDataTableEntity -Entity <Hashtable[]> -TableName <String> -SharedAccessSignature <Uri>
 [<CommonParameters>]
```

### Key
```
Update-AzDataTableEntity -Entity <Hashtable[]> -TableName <String> -StorageAccountName <String>
 -StorageAccountKey <String> [<CommonParameters>]
```

## DESCRIPTION
Update one or more entities already existing in an Azure Table.
For adding and overwriting, also see the command Add-AzDataTableEntity.

The PartitionKey and RowKey cannot be updated.

## EXAMPLES

### Example 1
```powershell
PS C:\> $UserEntity = Get-AzDataTableEntity -Filter "FirstName eq 'Bobby'" -TableName $TableName -ConnectionString $ConnectionString
PS C:\> $UserEntity['LastName'] = 'Tables'
PS C:\> Update-AzDataTableEntity -Entity $UserEntity -TableName $TableName -ConnectionString $ConnectionString
```

Update the last name of the user "Bobby" to "Tables" using a connection string.

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

### -Entity
The entities to update in the table.

```yaml
Type: Hashtable[]
Parameter Sets: (All)
Aliases: Row, Entry, Property

Required: True
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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Collections.Hashtable[]

## OUTPUTS

## NOTES

## RELATED LINKS
