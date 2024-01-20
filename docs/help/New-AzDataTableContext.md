---
external help file: AzBobbyTables.PS.dll-Help.xml
Module Name: AzBobbyTables
online version:
schema: 2.0.0
---

# New-AzDataTableContext

## SYNOPSIS

Creates a context object with authentication information for the table to operate on, to be used in other commands.

## SYNTAX

### ConnectionString
```
New-AzDataTableContext -TableName <String> -ConnectionString <String>
 [<CommonParameters>]
```

### SAS
```
New-AzDataTableContext -TableName <String> -SharedAccessSignature <Uri>
 [<CommonParameters>]
```

### Key
```
New-AzDataTableContext -TableName <String> -StorageAccountName <String> -StorageAccountKey <String>
 [<CommonParameters>]
```

### Token
```
New-AzDataTableContext -TableName <String> -StorageAccountName <String> -Token <String>
 [<CommonParameters>]
```

### ManagedIdentity
```
New-AzDataTableContext -TableName <String> -StorageAccountName <String> [-ManagedIdentity] [-ClientId <String>]
 [<CommonParameters>]
```

## DESCRIPTION

Creates a context object with authentication information for the table to operate on, to be used in other commands.

## EXAMPLES

### Example 1

```powershell
PS C:\> $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
```

Creates a context object using the table name and a connection string.

### Example 2

```powershell
PS C:\> $Context = New-AzDataTableContext -TableName $TableName -StorageAccountName $Name -StorageAccountKey $Key
```

Creates a context object using the table name, storage account name and a storage account access key.

### Example 3

```powershell
PS C:\> $Context = New-AzDataTableContext -TableName $TableName -SharedAccessSignature $SAS
```

Creates a context object using the table name and a shared access signature URL.

### Example 4

```powershell
PS C:\> $Context = New-AzDataTableContext -TableName $TableName -StorageAccountName $Name -ManagedIdentity
```

Creates a context object using the table name, storage account name and a managed identity for authorization.

### Example 5

```powershell
PS C:\> $Context = New-AzDataTableContext -TableName $TableName -StorageAccountName $Name -Token $Token
```

Creates a context object using the table name, storage account name and an access token.

## PARAMETERS

### -ClientId

Specifies the client id when using a user-assigned managed identity.

```yaml
Type: String
Parameter Sets: ManagedIdentity
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

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

### -SharedAccessSignature

The table service SAS URL. The table endpoint of the storage account, with the shared access token appended to it.

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

An access token to use for authorization.

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
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutBuffer, -OutVariable, -PipelineVariable, -ProgressAction, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None

## OUTPUTS

### System.Object

## NOTES

## RELATED LINKS
