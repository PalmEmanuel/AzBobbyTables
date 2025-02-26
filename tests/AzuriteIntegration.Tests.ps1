Describe 'Azurite Integration Tests' -Tag 'Integration' {
    BeforeAll {
        function Get-ComparableHash {
            param($InputObject)

            $PropertyHash = @{}

            # Input is either hashtable or pscustomobject
            if ($InputObject -is [hashtable]) {
                $PropertyHash = $InputObject.GetEnumerator() | Sort-Object { $_.Key } | ForEach-Object -Begin {
                    $Hash = [ordered]@{}
                } -Process {
                    $PropertyName = $_.Key
                    $PropertyValue = $_.Value
                    if (![string]::IsNullOrWhiteSpace($PropertyValue)) {
                        $Hash.Add($PropertyName, $PropertyValue)
                    }
                } -End {
                    Write-Output $Hash
                }
            }
            elseif ($InputObject -is [pscustomobject]) {
                $PropertyHash = $InputObject | Get-Member -MemberType NoteProperty | Sort-Object { $_.Name } | ForEach-Object -Begin {
                    $Hash = [ordered]@{}
                } -Process {
                    $PropertyName = $_.Name
                    $PropertyValue = $InputObject."$($_.Name)"
                    if (![string]::IsNullOrWhiteSpace($PropertyValue)) {
                        $Hash.Add($PropertyName, $PropertyValue)
                    }
                } -End {
                    Write-Output $Hash
                }
            }
            
            # Output hash of object
            [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes(($PropertyHash.Keys | Sort-Object | ForEach-Object {
                            $PropertyHash[$_]
                        }) -join ''))
        }
    }

    Context 'hashtable' {
        BeforeAll {
            $null = Start-ThreadJob { & azurite --silent --location "$using:TestDrive/hashtable" } -Name 'Azurite'

            $UsersHashtable = 1..4 | ForEach-Object {
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
            $MissingPartitionKeyUsersHashtable = 1..4 | ForEach-Object {
                @{
                    'RowKey'       = "$_"
                    'FirstName'    = "Bobby$_"
                    'LastName'     = "Tables$_"
                    'Id'           = "$_"
                    'Value1'       = 1111111111
                    'Prop1'        = 'Lorem ipsum.'
                }
            }
            $MissingRowKeyUsersHashtable = 1..4 | ForEach-Object {
                @{
                    'PartitionKey' = 'AzBobbyTables'
                    'FirstName'    = "Bobby$_"
                    'LastName'     = "Tables$_"
                    'Id'           = "$_"
                    'Value1'       = 1111111111
                    'Prop1'        = 'Lorem ipsum.'
                }
            }
            $InvalidCasingUsersHashtable = 1..4 | ForEach-Object {
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
            $UpdatedUsersHashtable = 1..4 | ForEach-Object {
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
            $UsersToRemoveHashtable = 1..2 | ForEach-Object {
                @{
                    'PartitionKey' = 'AzBobbyTables'
                    'RowKey'       = "$_"
                }
            }

            $TableName = 'AzBobbyTablesHashtable'
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
            { Add-AzDataTableEntity -Context $Context -Entity $MissingPartitionKeyUsersHashtable } | Should -Throw -Because 'The data is missing PartitionKey'
            { Add-AzDataTableEntity -Context $Context -Entity $MissingRowKeyUsersHashtable } | Should -Throw -Because 'The data is missing RowKey'
            { Add-AzDataTableEntity -Context $Context -Entity $InvalidCasingUsersHashtable } | Should -Throw -Because 'The keys in the data have invalid casing'
        }

        It 'can add data' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            $AddResult = Add-AzDataTableEntity -Context $Context -Entity $UsersHashtable
            $AddResult | Should -BeNullOrEmpty
            $Result = Get-AzDataTableEntity -Context $Context | Select-Object -ExcludeProperty 'Timestamp', 'ETag'
            foreach ($User in $Result) {
                $ExpectedData = Get-ComparableHash ($UsersHashtable | Where-Object { $_['Id'] -eq $User.Id })
                Get-ComparableHash $User | Should -Be $ExpectedData
            }
        }

        It 'cannot update entities using invalid data' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            { Update-AzDataTableEntity -Context $Context -Entity $MissingPartitionKeyUsersHashtable } | Should -Throw -Because 'The data is missing PartitionKey'
            { Update-AzDataTableEntity -Context $Context -Entity $MissingRowKeyUsersHashtable } | Should -Throw -Because 'The data is missing RowKey'
            { Update-AzDataTableEntity -Context $Context -Entity $InvalidCasingUsersHashtable } | Should -Throw -Because 'The keys in the data have invalid casing'
        }

        It 'can update entities' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            (Get-AzDataTableEntity -Context $Context -Filter 'Value1 eq 1111111111').Count | Should -BeExactly $UsersHashtable.Count
            $UpdateResult = Update-AzDataTableEntity -Context $Context -Entity $UpdatedUsersHashtable
            $UpdateResult | Should -BeNullOrEmpty
            Get-AzDataTableEntity -Context $Context -Filter 'Value1 eq 1111111111' | Should -BeNullOrEmpty
            $Result = Get-AzDataTableEntity -Context $Context | Select-Object -ExcludeProperty 'Timestamp', 'ETag'
            foreach ($User in $Result) {
                $ExpectedData = Get-ComparableHash ($UpdatedUsersHashtable | Where-Object { $_['Id'] -eq $User.Id })
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
            { Remove-AzDataTableEntity -Context $Context -Entity $MissingPartitionKeyUsersHashtable } | Should -Throw -Because 'The data is missing PartitionKey'
            { Remove-AzDataTableEntity -Context $Context -Entity $MissingRowKeyUsersHashtable } | Should -Throw -Because 'The data is missing RowKey'
            { Remove-AzDataTableEntity -Context $Context -Entity $InvalidCasingUsersHashtable } | Should -Throw -Because 'The keys in the data have invalid casing'
        }

        It 'can remove entities' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            Remove-AzDataTableEntity -Context $Context -Entity $UsersToRemoveHashtable | Should -BeNullOrEmpty
            (Get-AzDataTableEntity -Context $Context).Count | Should -BeExactly ($UsersHashtable.Count - $UsersToRemoveHashtable.Count)
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
            Add-AzDataTableEntity -Context $Context -Entity $UsersHashtable -CreateTableIfNotExists | Should -BeNullOrEmpty
            $Result = Get-AzDataTableEntity -Context $Context | Select-Object -ExcludeProperty 'Timestamp', 'ETag'
            foreach ($User in $Result) {
                $ExpectedData = Get-ComparableHash ($UsersHashtable | Where-Object { $_['Id'] -eq $User.Id })
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
            Stop-Job -Name 'Azurite'
            Remove-Job -Name 'Azurite'
        }
    }
    
    Context 'psobject' {
        BeforeAll {
            $null = Start-ThreadJob { & azurite --silent --location "$using:TestDrive/psobject" } -Name 'Azurite'

            $UsersPSObjects = 1..4 | ForEach-Object {
                [pscustomobject]@{
                    'PartitionKey' = 'AzBobbyTables'
                    'RowKey'       = "$_"
                    'FirstName'    = "Bobby$_"
                    'LastName'     = "Tables$_"
                    'Id'           = "$_"
                    'Value1'       = 1111111111
                    'Prop1'        = 'Lorem ipsum.'
                }
            }
            $MissingPartitionKeyUsersPSObjects = 1..4 | ForEach-Object {
                [pscustomobject]@{
                    'RowKey'       = "$_"
                    'FirstName'    = "Bobby$_"
                    'LastName'     = "Tables$_"
                    'Id'           = "$_"
                    'Value1'       = 1111111111
                    'Prop1'        = 'Lorem ipsum.'
                }
            }
            $MissingRowKeyUsersPSObjects = 1..4 | ForEach-Object {
                [pscustomobject]@{
                    'PartitionKey' = 'AzBobbyTables'
                    'FirstName'    = "Bobby$_"
                    'LastName'     = "Tables$_"
                    'Id'           = "$_"
                    'Value1'       = 1111111111
                    'Prop1'        = 'Lorem ipsum.'
                }
            }
            $InvalidCasingUsersPSObjects = 1..4 | ForEach-Object {
                [pscustomobject]@{
                    'partitionKey' = 'AzBobbyTables'
                    'rowkey'       = "$_"
                    'FirstName'    = "Bobby$_"
                    'LastName'     = "Tables$_"
                    'Id'           = "$_"
                    'Value1'       = 1111111111
                    'Prop1'        = 'Lorem ipsum.'
                }
            }
            $UpdatedUsersPSObjects = 1..4 | ForEach-Object {
                [pscustomobject]@{
                    'PartitionKey' = 'AzBobbyTables'
                    'RowKey'       = "$_"
                    'FirstName'    = 'Bobby'
                    'LastName'     = 'Tables'
                    'Id'           = "$_"
                    'Value1'       = 2222222222
                    'Prop1'        = 'Hello world.'
                }
            }
            $UsersToRemovePSObjects = 1..2 | ForEach-Object {
                [pscustomobject]@{
                    'PartitionKey' = 'AzBobbyTables'
                    'RowKey'       = "$_"
                }
            }

            $TableName = 'AzBobbyTablesPSObject'
            $ConnectionString = 'UseDevelopmentStorage=true'
        }

        It 'can create table' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            New-AzDataTable -Context $Context | Should -BeNullOrEmpty
        }

        It 'cannot add invalid data' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            { Add-AzDataTableEntity -Context $Context -Entity $MissingPartitionKeyUsersPSObjects } | Should -Throw -Because 'The data is missing PartitionKey'
            { Add-AzDataTableEntity -Context $Context -Entity $MissingRowKeyUsersPSObjects } | Should -Throw -Because 'The data is missing RowKey'
            { Add-AzDataTableEntity -Context $Context -Entity $InvalidCasingUsersPSObjects } | Should -Throw -Because 'The keys in the data have invalid casing'
        }

        It 'can add data' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            Add-AzDataTableEntity -Context $Context -Entity $UsersPSObjects | Should -BeNullOrEmpty
            $Result = Get-AzDataTableEntity -Context $Context | Select-Object -ExcludeProperty 'Timestamp', 'ETag'
            foreach ($User in $Result) {
                $ExpectedData = Get-ComparableHash ($UsersPSObjects | Where-Object { $_.Id -eq $User.Id })
                Get-ComparableHash $User | Should -Be $ExpectedData
            }
        }

        It 'cannot update entities using invalid data' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            { Update-AzDataTableEntity -Context $Context -Entity $MissingPartitionKeyUsersPSObjects } | Should -Throw -Because 'The data is missing PartitionKey'
            { Update-AzDataTableEntity -Context $Context -Entity $MissingRowKeyUsersPSObjects } | Should -Throw -Because 'The data is missing RowKey'
            { Update-AzDataTableEntity -Context $Context -Entity $InvalidCasingUsersPSObjects } | Should -Throw -Because 'The keys in the data have invalid casing'
        }

        It 'can update entities' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            (Get-AzDataTableEntity -Context $Context -Filter 'Value1 eq 1111111111').Count | Should -BeExactly $UsersPSObjects.Count
            Update-AzDataTableEntity -Context $Context -Entity $UpdatedUsersPSObjects | Should -BeNullOrEmpty
            Get-AzDataTableEntity -Context $Context -Filter 'Value1 eq 1111111111' | Should -BeNullOrEmpty
            $Result = Get-AzDataTableEntity -Context $Context | Select-Object -ExcludeProperty 'Timestamp', 'ETag'
            foreach ($User in $Result) {
                $ExpectedData = Get-ComparableHash ($UpdatedUsersPSObjects | Where-Object { $_.Id -eq $User.Id })
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

        It 'cannot remove entities with invalid data' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            { Remove-AzDataTableEntity -Context $Context -Entity $MissingPartitionKeyUsersPSObjects } | Should -Throw -Because 'The data is missing PartitionKey'
            { Remove-AzDataTableEntity -Context $Context -Entity $MissingRowKeyUsersPSObjects } | Should -Throw -Because 'The data is missing RowKey'
            { Remove-AzDataTableEntity -Context $Context -Entity $InvalidCasingUsersPSObjects } | Should -Throw -Because 'The keys in the data have invalid casing'
        }

        It 'can remove entities' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            Remove-AzDataTableEntity -Context $Context -Entity $UsersToRemovePSObjects | Should -BeNullOrEmpty
            (Get-AzDataTableEntity -Context $Context).Count | Should -BeExactly ($UsersPSObjects.Count - $UsersToRemovePSObjects.Count)
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
            Remove-AzDataTable -Context $Context | Should -BeNullOrEmpty
        }

        It 'cannot use removed table' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            { Get-AzDataTableEntity -Context $Context } | Should -Throw
        }

        It 'can add data to new table when forcing creation' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            Add-AzDataTableEntity -Context $Context -Entity $UsersPSObjects -CreateTableIfNotExists | Should -BeNullOrEmpty
            $Result = Get-AzDataTableEntity -Context $Context | Select-Object -ExcludeProperty 'Timestamp', 'ETag'
            foreach ($User in $Result) {
                $ExpectedData = Get-ComparableHash ($UsersPSObjects | Where-Object { $_.Id -eq $User.Id })
                Get-ComparableHash $User | Should -Be $ExpectedData
            }
        }

        AfterAll {
            Stop-Job -Name 'Azurite'
            Remove-Job -Name 'Azurite'
        }
    }
}