Describe 'Azurite Integration Tests' -Tag 'Integration' {
    BeforeAll {

        function New-TestData {
            param(
                [string]$EntityType,
                [string]$DataType,
                [int[]]$IdRange = @(1..4)
            )
            
            $data = switch ($DataType) {
                'Users' {
                    $IdRange | ForEach-Object {
                        @{
                            'PartitionKey' = 'AzBobbyTables'
                            'RowKey'       = "$_"
                            'FirstName'    = "Bobby$_"
                            'LastName'     = "Tables$_"
                            'Id'           = "$_"
                            'Value1'       = 1111111111
                            'Prop1'        = 'Lorem ipsum.'
                        }
                    }
                }
                'MissingPartitionKey' {
                    $IdRange | ForEach-Object {
                        @{
                            'RowKey'    = "$_"
                            'FirstName' = "Bobby$_"
                            'LastName'  = "Tables$_"
                            'Id'        = "$_"
                            'Value1'    = 1111111111
                            'Prop1'     = 'Lorem ipsum.'
                        }
                    }
                }
                'MissingRowKey' {
                    $IdRange | ForEach-Object {
                        @{
                            'PartitionKey' = 'AzBobbyTables'
                            'FirstName'    = "Bobby$_"
                            'LastName'     = "Tables$_"
                            'Id'           = "$_"
                            'Value1'       = 1111111111
                            'Prop1'        = 'Lorem ipsum.'
                        }
                    }
                }
                'InvalidCasing' {
                    $IdRange | ForEach-Object {
                        @{
                            'partitionKey' = 'AzBobbyTables'
                            'rowkey'       = "$_"
                            'FirstName'    = "Bobby$_"
                            'LastName'     = "Tables$_"
                            'Id'           = "$_"
                            'Value1'       = 1111111111
                            'Prop1'        = 'Lorem ipsum.'
                        }
                    }
                }
                'Updated' {
                    $IdRange | ForEach-Object {
                        @{
                            'PartitionKey' = 'AzBobbyTables'
                            'RowKey'       = "$_"
                            'FirstName'    = 'Bobby'
                            'LastName'     = 'Tables'
                            'Id'           = "$_"
                            'Value1'       = 2222222222
                            'Prop1'        = 'Hello world.'
                        }
                    }
                }
                'ToRemove' {
                    $IdRange | ForEach-Object {
                        @{
                            'PartitionKey' = 'AzBobbyTables'
                            'RowKey'       = "$_"
                        }
                    }
                }
            }

            # Convert to the specified entity type
            switch ($EntityType) {
                'Hashtable' {
                    return $data
                }
                'PSObject' {
                    return $data | ForEach-Object { [pscustomobject]$_ }
                }
                'SortedList' {
                    return $data | ForEach-Object {
                        $sortedList = [System.Collections.SortedList]::new()
                        foreach ($key in $_.Keys) {
                            $sortedList.Add($key, $_[$key])
                        }
                        $sortedList
                    }
                }
            }
        }
        function Get-ComparableHash {
            param($InputObject)

            # Convert any object to a normalized hashtable for comparison
            $NormalizedHash = @{}

            # Use PowerShell's reflection to dynamically get all properties regardless of type
            switch ($InputObject) {
                # Handle hashtables and similar key-value collections
                { $_ -is [System.Collections.IDictionary] } {
                    foreach ($key in $_.Keys) {
                        $value = $_[$key]
                        if ($null -ne $value -and $value -ne '') {
                            $NormalizedHash[$key] = $value
                        }
                    }
                }
                # Handle PSObjects and any other object with properties
                default {
                    # Use Get-Member to dynamically discover all properties
                    $properties = $_ | Get-Member -MemberType Properties, NoteProperty | Where-Object { $_.Name -notmatch '^PS' }
                    foreach ($prop in $properties) {
                        try {
                            $value = $_.$($prop.Name)
                            if ($null -ne $value -and $value -ne '') {
                                $NormalizedHash[$prop.Name] = $value
                            }
                        }
                        catch {
                            # Skip properties that can't be accessed
                            continue
                        }
                    }
                }
            }

            # Create a consistent string representation by sorting keys and concatenating values
            $sortedKeys = $NormalizedHash.Keys | Sort-Object
            $valueString = ($sortedKeys | ForEach-Object { $NormalizedHash[$_] }) -join ''
            
            # Return base64 encoded hash for comparison
            [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($valueString))
        }

        $EntityTypes = @('Hashtable', 'PSObject', 'SortedList')

        # Start single Azurite instance for all tests
        $AzuriteLocation = "$TestDrive/azurite"
        if (Test-Path $AzuriteLocation) {
            Remove-Item $AzuriteLocation -Recurse -Force -ErrorAction SilentlyContinue
        }
        New-Item -Path $AzuriteLocation -ItemType Directory -Force | Out-Null
        
        $null = Start-ThreadJob { 
            & azurite --silent --location $using:AzuriteLocation
        } -Name 'Azurite'
        
        # Wait for Azurite to start
        Start-Sleep -Milliseconds 1000
    }

    BeforeDiscovery {
        # Test cases for different entity types
        $TestCases = @(
            @{ EntityType = 'Hashtable'; TypeName = 'hashtable' }
            @{ EntityType = 'PSObject'; TypeName = 'psobject' }
            @{ EntityType = 'SortedList'; TypeName = 'sortedlist' }
        )
    }

    Context 'Entity Type <EntityType>' -ForEach $TestCases {
        BeforeAll {
            # Generate test data for this entity type
            $Users = New-TestData -EntityType $EntityType -DataType 'Users'
            $MissingPartitionKeyUsers = New-TestData -EntityType $EntityType -DataType 'MissingPartitionKey'
            $MissingRowKeyUsers = New-TestData -EntityType $EntityType -DataType 'MissingRowKey'
            $InvalidCasingUsers = New-TestData -EntityType $EntityType -DataType 'InvalidCasing'
            $UpdatedUsers = New-TestData -EntityType $EntityType -DataType 'Updated'
            $UsersToRemove = New-TestData -EntityType $EntityType -DataType 'ToRemove' -IdRange @(1..2)

            # Use unique table name for each entity type to ensure isolation
            $TableName = "AzBobbyTables$EntityType"
            # Single connection string for all tests
            $ConnectionString = 'UseDevelopmentStorage=true'
        }

        It 'can create table context with table name' {
            { New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString } | Should -Not -Throw
        }

        It 'can create table context without table name' {
            { New-AzDataTableContext -ConnectionString $ConnectionString } | Should -Not -Throw
        }

        It 'can create table with context containing table name' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            { $null = New-AzDataTable -Context $Context } | Should -Not -Throw
        }

        It 'cannot create table with context missing table name' {
            $Context = New-AzDataTableContext -ConnectionString $ConnectionString
            { $null = New-AzDataTable -Context $Context } | Should -Throw
        }

        It 'cannot add invalid data' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            { Add-AzDataTableEntity -Context $Context -Entity $MissingPartitionKeyUsers } | Should -Throw -Because 'The data is missing PartitionKey'
            { Add-AzDataTableEntity -Context $Context -Entity $MissingRowKeyUsers } | Should -Throw -Because 'The data is missing RowKey'
            { Add-AzDataTableEntity -Context $Context -Entity $InvalidCasingUsers } | Should -Throw -Because 'The keys in the data have invalid casing'
        }

        It 'can add data' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            $AddResult = Add-AzDataTableEntity -Context $Context -Entity $Users
            $AddResult | Should -BeNullOrEmpty
            $Result = Get-AzDataTableEntity -Context $Context | Select-Object -ExcludeProperty 'Timestamp', 'ETag'
            foreach ($User in $Result) {
                # Get the user ID from the result object (which is always a PSObject when returned)
                $UserId = $User.Id
                
                $ExpectedData = Get-ComparableHash ($Users | Where-Object { 
                        if ($_ -is [hashtable]) { $_['Id'] -eq $UserId }
                        elseif ($_ -is [pscustomobject]) { $_.Id -eq $UserId }
                        elseif ($_ -is [System.Collections.SortedList]) { $_['Id'] -eq $UserId }
                    })
                Get-ComparableHash $User | Should -Be $ExpectedData
            }
        }

        It 'cannot update entities using invalid data' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            { Update-AzDataTableEntity -Context $Context -Entity $MissingPartitionKeyUsers } | Should -Throw -Because 'The data is missing PartitionKey'
            { Update-AzDataTableEntity -Context $Context -Entity $MissingRowKeyUsers } | Should -Throw -Because 'The data is missing RowKey'
            { Update-AzDataTableEntity -Context $Context -Entity $InvalidCasingUsers } | Should -Throw -Because 'The keys in the data have invalid casing'
        }

        It 'can update entities' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            (Get-AzDataTableEntity -Context $Context -Filter 'Value1 eq 1111111111').Count | Should -BeExactly $Users.Count
            $UpdateResult = Update-AzDataTableEntity -Context $Context -Entity $UpdatedUsers
            $UpdateResult | Should -BeNullOrEmpty
            Get-AzDataTableEntity -Context $Context -Filter 'Value1 eq 1111111111' | Should -BeNullOrEmpty
            $Result = Get-AzDataTableEntity -Context $Context | Select-Object -ExcludeProperty 'Timestamp', 'ETag'
            foreach ($User in $Result) {
                # Get the user ID from the result object (which is always a PSObject when returned)
                $UserId = $User.Id
                
                $ExpectedData = Get-ComparableHash ($UpdatedUsers | Where-Object { 
                        if ($_ -is [hashtable]) { $_['Id'] -eq $UserId }
                        elseif ($_ -is [pscustomobject]) { $_.Id -eq $UserId }
                        elseif ($_ -is [System.Collections.SortedList]) { $_['Id'] -eq $UserId }
                    })
                Get-ComparableHash $User | Should -Be $ExpectedData
            }
        }

        It 'cannot update changed entities without -Force' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            $Users1 = Get-AzDataTableEntity -Context $Context
            $Users2 = Get-AzDataTableEntity -Context $Context
            $Users1 = $Users1 | ForEach-Object {
                $_.Value1 = 'Updated'
                $_
            }
            $Users2 = $Users2 | ForEach-Object {
                $_.Prop1 = 'Will conflict with update from Result1'
                $_
            }
            Update-AzDataTableEntity -Context $Context -Entity $Users1 | Should -BeNullOrEmpty
            { Update-AzDataTableEntity -Context $Context -Entity $Users2 } | Should -Throw
            (Get-AzDataTableEntity -Context $Context -Filter "Value1 eq 'Updated'").Count | Should -Be $Users1.Count
            { Update-AzDataTableEntity -Context $Context -Entity $Users2 -Force } | Should -Not -Throw
            (Get-AzDataTableEntity -Context $Context -Filter "Value1 eq 'Updated'").Count | Should -Be 0
        }

        It 'can get count of entities' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            Get-AzDataTableEntity -Context $Context -Count | Should -BeExactly 4
        }

        It 'cannot remove entities using invalid data' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            { Remove-AzDataTableEntity -Context $Context -Entity $MissingPartitionKeyUsers } | Should -Throw -Because 'The data is missing PartitionKey'
            { Remove-AzDataTableEntity -Context $Context -Entity $MissingRowKeyUsers } | Should -Throw -Because 'The data is missing RowKey'
            { Remove-AzDataTableEntity -Context $Context -Entity $InvalidCasingUsers } | Should -Throw -Because 'The keys in the data have invalid casing'
        }

        It 'can remove entities' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            Remove-AzDataTableEntity -Context $Context -Entity $UsersToRemove | Should -BeNullOrEmpty
            (Get-AzDataTableEntity -Context $Context).Count | Should -BeExactly ($Users.Count - $UsersToRemove.Count)
        }

        It 'cannot remove changed entities without -Force' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            $Users1 = Get-AzDataTableEntity -Context $Context
            $Users2 = Get-AzDataTableEntity -Context $Context
            $Users1 = $Users1 | ForEach-Object {
                $_.Value1 = 'Updated'
                $_
            }
            Update-AzDataTableEntity -Context $Context -Entity $Users1 | Should -BeNullOrEmpty
            { Remove-AzDataTableEntity -Context $Context -Entity $Users2 } | Should -Throw
            (Get-AzDataTableEntity -Context $Context -Filter "Value1 eq 'Updated'").Count | Should -Be $Users1.Count
            { Remove-AzDataTableEntity -Context $Context -Entity $Users2 -Force } | Should -Not -Throw
            (Get-AzDataTableEntity -Context $Context).Count | Should -Be 0
            # Restore table for next test
            Add-AzDataTableEntity -Context $Context -Entity $Users2 | Should -BeNullOrEmpty
        }

        It 'can clear table' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            Get-AzDataTableEntity -Context $Context | Should -Not -BeNullOrEmpty
            Clear-AzDataTable -Context $Context
            Get-AzDataTableEntity -Context $Context | Should -BeNullOrEmpty
        }

        It 'can remove table' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            { Remove-AzDataTable -Context $Context } | Should -Not -Throw
        }

        It 'cannot use removed table' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            { Get-AzDataTableEntity -Context $Context } | Should -Throw
        }

        It 'can add data to new table when forcing creation' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            Add-AzDataTableEntity -Context $Context -Entity $Users -CreateTableIfNotExists | Should -BeNullOrEmpty
            $Result = Get-AzDataTableEntity -Context $Context | Select-Object -ExcludeProperty 'Timestamp', 'ETag'
            foreach ($User in $Result) {
                # Get the user ID from the result object (which is always a PSObject when returned)
                $UserId = $User.Id
                
                $ExpectedData = Get-ComparableHash ($Users | Where-Object { 
                        if ($_ -is [hashtable]) { $_['Id'] -eq $UserId }
                        elseif ($_ -is [pscustomobject]) { $_.Id -eq $UserId }
                        elseif ($_ -is [System.Collections.SortedList]) { $_['Id'] -eq $UserId }
                    })
                Get-ComparableHash $User | Should -Be $ExpectedData
            }
            { Remove-AzDataTable -Context $Context } | Should -Not -Throw
        }

        It 'can get all tables from storage account with context containing table name' {
            $Context = New-AzDataTableContext -TableName "${TableName}1" -ConnectionString $ConnectionString
            { $null = New-AzDataTable -Context $Context } | Should -Not -Throw
            $Context = New-AzDataTableContext -TableName "${TableName}2" -ConnectionString $ConnectionString
            { $null = New-AzDataTable -Context $Context } | Should -Not -Throw
            $Tables = Get-AzDataTable -Context $Context
            $Tables | Should -Contain "${TableName}1"
            $Tables | Should -Contain "${TableName}2"
            $Tables.Count | Should -BeExactly 2
        }

        It 'can get all tables from storage account with context missing table name' {
            $Context = New-AzDataTableContext -ConnectionString $ConnectionString
            $Tables = Get-AzDataTable -Context $Context
            $Tables | Should -Contain "${TableName}1"
            $Tables | Should -Contain "${TableName}2"
            $Tables.Count | Should -BeExactly 2
        }

        It 'can get specific table with filter from storage account with context containing table name' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            $Table = Get-AzDataTable -Context $Context -Filter "TableName eq '${TableName}1'"
            $Table | Should -BeExactly "${TableName}1"
        }

        It 'can get specific table with filter from storage account with context missing table name' {
            $Context = New-AzDataTableContext -ConnectionString $ConnectionString
            $Table = Get-AzDataTable -Context $Context -Filter "TableName eq '${TableName}2'"
            $Table | Should -BeExactly "${TableName}2"
        }

        AfterAll {
            # Clean up tables created by this entity type test
            try {
                $Context = New-AzDataTableContext -ConnectionString $ConnectionString
                $Tables = Get-AzDataTable -Context $Context | Where-Object { $_ -like "*$EntityType*" }
                foreach ($Table in $Tables) {
                    try {
                        $TableContext = New-AzDataTableContext -TableName $Table -ConnectionString $ConnectionString
                        Remove-AzDataTable -Context $TableContext -ErrorAction SilentlyContinue
                    }
                    catch {
                        # Ignore cleanup errors
                    }
                }
            }
            catch {
                # Ignore cleanup errors
            }
        }
    }

    AfterAll {
        # Stop the single Azurite instance
        Stop-Job -Name 'Azurite' -ErrorAction SilentlyContinue
        Remove-Job -Name 'Azurite' -ErrorAction SilentlyContinue
        
        # Clean up the Azurite data directory
        $AzuriteLocation = "$TestDrive/azurite"
        if (Test-Path $AzuriteLocation) {
            Remove-Item $AzuriteLocation -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    Context 'Type Coverage Validation' {
        It 'should have integration tests for all supported entity types' {
            # Get the list of supported entity types from the module
            $SupportedTypes = Get-AzDataTableSupportedEntityType
            
            # Ensure we have a test case for each supported type
            foreach ($SupportedType in $SupportedTypes) {
                $EntityTypes | Should -Contain $SupportedType -Because "Integration tests should cover all supported entity types. Missing test for: $SupportedType"
            }
            
            # Ensure we don't have tests for unsupported types
            foreach ($TestedType in $EntityTypes) {
                $SupportedTypes | Should -Contain $TestedType -Because "Integration tests should only test supported entity types. Found unexpected test for: $TestedType"
            }
        }
    }
}