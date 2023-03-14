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

### TableOperation

```powershell
Add-AzDataTableEntity -Entity <Hashtable[]> [-Force] -Context <AzDataTableContext> [-CreateTableIfNotExists]
 [<CommonParameters>]
```

### Count

```powershell
Add-AzDataTableEntity -Context <AzDataTableContext> [-CreateTableIfNotExists] [<CommonParameters>]
```

## DESCRIPTION

Add one or more entities to an Azure Table, provided as hashtables.

## EXAMPLES

### Example 1

```powershell
PS C:\> $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
PS C:\> $User = @{ FirstName = 'Bobby'; LastName = 'Tables'; PartitionKey = 'Example'; RowKey = '1' }
PS C:\> Add-AzDataTableEntity -Entity $User -Context $Context
```

Add the user "Bobby Tables" to a table using a connection string.

### Example 2

```powershell
PS C:\> $Context = New-AzDataTableContext -TableName $TableName -SharedAccessSignature $SAS
PS C:\> $Users = @(
>>  @{ FirstName = 'Bobby'; LastName = 'Tables'; PartitionKey = 'Example'; RowKey = '1' },
>>  @{ FirstName = 'Bobby Junior'; LastName = 'Tables'; PartitionKey = 'Example'; RowKey = '2' } )
PS C:\> Add-AzDataTableEntity -Entity $Users -Context $Context -Force
```

Add multiple users to a table using a shared access signature URL, overwriting any existing rows.

## PARAMETERS

### -Entity

The entities to add to the table.

```yaml
Type: Hashtable[]
Parameter Sets: TableOperation
Aliases:

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
Parameter Sets: TableOperation
Aliases:

Required: False
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

### System.Collections.Hashtable[]

## OUTPUTS

## NOTES

## RELATED LINKS
