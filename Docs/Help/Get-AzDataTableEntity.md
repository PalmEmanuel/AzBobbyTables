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
Get-AzDataTableEntity [-Filter <String>] [-Property <String[]>] [-First <Int32>] [-Skip <Int32>]
 [-Sort <String[]>] -TableName <String> [-CreateTableIfNotExists] -ConnectionString <String>
 [<CommonParameters>]
```

### SAS
```
Get-AzDataTableEntity [-Filter <String>] [-Property <String[]>] [-First <Int32>] [-Skip <Int32>]
 [-Sort <String[]>] -TableName <String> [-CreateTableIfNotExists] -SharedAccessSignature <Uri>
 [<CommonParameters>]
```

### Key
```
Get-AzDataTableEntity [-Filter <String>] [-Property <String[]>] [-First <Int32>] [-Skip <Int32>]
 [-Sort <String[]>] -TableName <String> [-CreateTableIfNotExists] -StorageAccountName <String>
 -StorageAccountKey <String> [<CommonParameters>]
```

### Token
```
Get-AzDataTableEntity [-Filter <String>] [-Property <String[]>] [-First <Int32>] [-Skip <Int32>]
 [-Sort <String[]>] -TableName <String> [-CreateTableIfNotExists] -StorageAccountName <String> -Token <String>
 [<CommonParameters>]
```

### ManagedIdentity
```
Get-AzDataTableEntity [-Filter <String>] [-Property <String[]>] [-First <Int32>] [-Skip <Int32>]
 [-Sort <String[]>] -TableName <String> [-CreateTableIfNotExists] -StorageAccountName <String>
 [-ManagedIdentity] [<CommonParameters>]
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

### Example 2
```powershell
PS C:\> $UserEntities = Get-AzDataTableEntity -Sort 'Id','Age' -First 100 -Skip 500 -TableName $TableName -ManagedIdentity -StorageAccountName $Name
```

Skipping the first 100 entities, get 500 entities sorted by id and age from the table using a managed identity for authorization.

### Example 3
```powershell
PS C:\> $UserEntities = Get-AzDataTableEntity -Property 'FirstName','Age' -TableName $TableName -SharedAccessSignature $SAS
```

Get only the properties "FirstName" and "Age" for all entities found in the table using a shared access signature URL.

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

### -Property
One or several names of properties, to specify data to return for the entities.

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

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
Parameter Sets: Key, Token, ManagedIdentity
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

### -Token
The token to use for authorization.

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

### -First
Gets only the specified number of objects. Enter the number of objects to get.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases: Top, Take

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -ManagedIdentity
Specifies that the command is run by a managed identity (such as in an Azure Function), and authorization will be handled using that identity.

```yaml
Type: SwitchParameter
Parameter Sets: ManagedIdentity
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Skip
Ignores the specified number of objects and then gets the remaining objects. Enter the number of objects to skip.

```yaml
Type: Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Sort
Specifies one or several property names that to sort the entities by. If several properties are provided, the entities are sorted in the order that the property names are provided.

Note that using this parameter may slow down the command a lot when working with large data sets!

```yaml
Type: String[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: True (ByValue)
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
