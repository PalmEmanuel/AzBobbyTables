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

### TableOperation (Default)
```
Get-AzDataTableEntity -Context <AzDataTableContext> [-Filter <String>] [-Property <String[]>] [-First <Int32>]
 [-Skip <Int32>] [-Sort <String[]>] [<CommonParameters>]
```

### Count
```
Get-AzDataTableEntity -Context <AzDataTableContext> [-Count]
 [<CommonParameters>]
```

## DESCRIPTION

Get either all entities from an Azure Table, or those matching a provided OData filter.

Documentation on querying tables and entities: <https://docs.microsoft.com/en-gb/rest/api/storageservices/querying-tables-and-entities>

## EXAMPLES

### Example 1

```powershell
PS C:\> $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
PS C:\> $UserEntity = Get-AzDataTableEntity -Filter "FirstName eq 'Bobby' and LastName eq 'Tables'" -Context $Context
```

Get the user "Bobby Tables" from the table using a connection string.

### Example 2

```powershell
PS C:\> $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
PS C:\> $UserCount = Get-AzDataTableEntity -Filter "LastName eq 'Tables'" -Context $Context -Count
```

Use the Count parameter to get only the number of users matching the filter, using a connection string.

### Example 3

```powershell
PS C:\> $Context = New-AzDataTableContext -TableName $TableName -ManagedIdentity -StorageAccountName $Name
PS C:\> $UserEntities = Get-AzDataTableEntity -Sort 'Id','Age' -First 100 -Skip 500 -Context $Context
```

Skipping the first 100 entities, get 500 entities sorted by id and age from the table using a managed identity for authorization.

### Example 4

```powershell
PS C:\> $Context = New-AzDataTableContext -TableName $TableName -SharedAccessSignature $SAS
PS C:\> $UserEntities = Get-AzDataTableEntity -Property 'FirstName','Age' -Context $Context
```

Get only the properties "FirstName" and "Age" for all entities found in the table using a shared access signature URL.

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

### -Count

Specifies to only get the number of matching entities in the table, and not the data itself.

```yaml
Type: SwitchParameter
Parameter Sets: Count
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Filter

The OData filter to use in the query.
Documentation on querying tables and entities: <https://docs.microsoft.com/en-gb/rest/api/storageservices/querying-tables-and-entities>

```yaml
Type: String
Parameter Sets: TableOperation
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -First

Gets only the specified number of objects. Enter the number of objects to get.

```yaml
Type: Int32
Parameter Sets: TableOperation
Aliases: Top, Take

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Property

One or several names of properties, to specify data to return for the entities.

```yaml
Type: String[]
Parameter Sets: TableOperation
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Skip

Ignores the specified number of objects and then gets the remaining objects. Enter the number of objects to skip.

```yaml
Type: Int32
Parameter Sets: TableOperation
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Sort

Specifies one or several property names that to sort the entities by. If several properties are provided, the entities are sorted in the order that the property names are provided.

Note that using this parameter may slow down the command a lot when working with large data sets!

```yaml
Type: String[]
Parameter Sets: TableOperation
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

### System.Management.Automation.PSObject

## NOTES

## RELATED LINKS
