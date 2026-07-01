---
external help file: AzBobbyTables.PS.dll-Help.xml
Module Name: AzBobbyTables
online version:
schema: 2.0.0
---

# New-AzDataTable

## SYNOPSIS

Create a new table.

## SYNTAX

```
New-AzDataTable -Context <AzDataTableContext> [-MaxRetries <Int32>]
 [<CommonParameters>]
```

## DESCRIPTION

Create a new table.

## EXAMPLES

### Example 1

```powershell
PS C:\> $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
PS C:\> New-AzDataTable -Context $Context
```

Create a new table using a connection string for the storage account.

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

### -MaxRetries
The number of times to retry the operation when the request is throttled by the service with an HTTP 429 response. Between attempts the module waits for the duration indicated by the service's Retry-After response. Defaults to 0, which disables retries.

```yaml
Type: Int32
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

### None

## NOTES

## RELATED LINKS
