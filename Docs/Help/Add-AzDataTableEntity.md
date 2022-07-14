---
external help file: AzBobbyTables.PS.dll-Help.xml
Module Name: AzBobbyTables
online version:
schema: 2.0.0
---

# Add-AzDataTableEntity

## SYNOPSIS
Add one or more entities to an Azure Table.

## SYNTAX

### ConnectionString
```
Add-AzDataTableEntity -Entity <Hashtable[]> [-Force] -TableName <String> -ConnectionString <String>
 [<CommonParameters>]
```

### SAS
```
Add-AzDataTableEntity -Entity <Hashtable[]> [-Force] -TableName <String> -SharedAccessSignature <Uri>
 [<CommonParameters>]
```

### Key
```
Add-AzDataTableEntity -Entity <Hashtable[]> [-Force] -TableName <String> -StorageAccountName <String>
 -StorageAccountKey <String> [<CommonParameters>]
```

## DESCRIPTION
Add one or more entities to an Azure Table, provided as hashtables.

## EXAMPLES

### Example 1
```powershell
PS C:\> $User = @{ FirstName = 'Bobby'; LastName = 'Tables'; PartitionKey = 'Example'; RowKey = '1' }
PS C:\> Add-AzDataTableEntity -Entity $User -TableName $TableName -SharedAccessSignature $SAS
```

Add the user "Bobby Tables" to a table using a shared access signature URL.

### Example 2
```powershell
PS C:\> $Users = @(
>>  @{ FirstName = 'Bobby'; LastName = 'Tables'; PartitionKey = 'Example'; RowKey = '1' },
>>  @{ FirstName = 'Bobby Junior'; LastName = 'Tables'; PartitionKey = 'Example'; RowKey = '2' } )
PS C:\> Add-AzDataTableEntity -Entity $Users -TableName $TableName -ConnectionString $ConnectionString -Force
```

Add multiple users to a table using a connection string, overwriting any existing rows.

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
The entities to add to the table.

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

### -Force
Overwrites provided entities if they exist.
The same as running the command Update-AzDataTableEntity.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: UpdateExisting

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
