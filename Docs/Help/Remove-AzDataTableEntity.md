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

### ConnectionString
```
Remove-AzDataTableEntity -Entity <Hashtable[]> -TableName <String> -ConnectionString <String>
 [<CommonParameters>]
```

### SAS
```
Remove-AzDataTableEntity -Entity <Hashtable[]> -TableName <String> -SharedAccessSignature <Uri>
 [<CommonParameters>]
```

### Key
```
Remove-AzDataTableEntity -Entity <Hashtable[]> -TableName <String> -StorageAccountName <String>
 -StorageAccountKey <String> [<CommonParameters>]
```

## DESCRIPTION
Remove one or more entities from an Azure Table, based on PartitionKey and RowKey.

## EXAMPLES

### Example 1
```powershell
PS C:\> $Entity = @{ PartitionKey = 'Example'; RowKey = '1' }
PS C:\> Remove-AzDataTableEntity -Entity $Entity -TableName $TableName -StorageAccountName $Name -StorageAccountKey $Key
```

Remove the entity with PartitionKey "Example" and RowKey "1", using the storage account name and an access key.

### Example 2
```powershell
PS C:\> $UserEntity = Get-AzDataTableEntity -Filter "FirstName eq 'Bobby' and LastName eq 'Tables'" -TableName $TableName -ConnectionString $ConnectionString
PS C:\> Remove-AzDataTableEntity -Entity $UserEntity -TableName $TableName -StorageAccountName $Name -StorageAccountKey $Key
```

Get the user "Bobby Tables" from the table using a connection string, then remove the user using the storage account name and an access key.

### Example 3
```powershell
PS C:\> $Users = Get-AzDataTableEntity -Filter "LastName eq 'Tables'" -TableName $TableName -ConnectionString $ConnectionString
PS C:\> Remove-AzDataTableEntity -Entity $Users -TableName $TableName -StorageAccountName $Name -StorageAccountKey $Key
```

Gets all users with the last name "Tables" from the table using a connection string, then removes the users using the storage account name and an access key.

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
The entities to remove from the table.

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
