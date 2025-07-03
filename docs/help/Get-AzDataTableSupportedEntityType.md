---
external help file: AzBobbyTables.PS.dll-Help.xml
Module Name: AzBobbyTables
online version:
schema: 2.0.0
---

# Get-AzDataTableSupportedEntityType

## SYNOPSIS

Gets the list of data types supported as input entities by the module for commands with the `-Entity` parameter.

## SYNTAX

```
Get-AzDataTableSupportedEntityType [<CommonParameters>]
```

## DESCRIPTION

The cmdlet retrieves a list of data types that are supported as input entities by the module for commands with the `-Entity` parameter, such as Hashtable, PSObject and SortedList.

## EXAMPLES

### Example 1
```powershell
PS C:\> Get-AzDataTableSupportedEntityType
```

This command retrieves and displays the list of supported entity types that can be used with the module.

## PARAMETERS

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable, -ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
## OUTPUTS

### System.Object
## NOTES

## RELATED LINKS
