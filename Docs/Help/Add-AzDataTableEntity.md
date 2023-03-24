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

```
Add-AzDataTableEntity -Context <AzDataTableContext> -Entity <Object[]> [-Force] [-CreateTableIfNotExists]
 [<CommonParameters>]
```

## DESCRIPTION

Add one or more entities to an Azure Table, as an array of either Hashtables or PSObjects.

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

### -Entity

The entities to add to the table.

```yaml
Type: Object[]
Parameter Sets: (All)
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Force

Overwrites provided entities if they exist.

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

### System.Collections.Hashtable[] or System.Management.Automation.PSObject[]

This cmdlet takes either an array of hashtables or psobjects as input to the Entity parameter, which can also be provided through the pipeline.

## OUTPUTS

### System.Object

## NOTES

## RELATED LINKS
