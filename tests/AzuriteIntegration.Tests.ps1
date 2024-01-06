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
            $null = Start-ThreadJob { & azurite --silent --location "$using:TestDrive\hashtable" } -Name 'Azurite'

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

        It 'can create table' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            { $null = New-AzDataTable -Context $Context } | Should -Not -Throw
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

        It 'can get count of entities' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            Get-AzDataTableEntity -Context $Context -Count | Should -BeExactly 4
        }

        It 'can remove entities' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            Remove-AzDataTableEntity -Context $Context -Entity $UsersToRemoveHashtable | Should -BeNullOrEmpty
            (Get-AzDataTableEntity -Context $Context).Count | Should -BeExactly ($UsersHashtable.Count - $UsersToRemoveHashtable.Count)
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
        }

        AfterAll {
            Stop-Job -Name 'Azurite'
            Remove-Job -Name 'Azurite'
        }
    }
    
    Context 'psobject' {
        BeforeAll {
            $null = Start-ThreadJob { & azurite --silent --location "$using:TestDrive\hashtable" } -Name 'Azurite'

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

        It 'can add data' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            Add-AzDataTableEntity -Context $Context -Entity $UsersPSObjects | Should -BeNullOrEmpty
            $Result = Get-AzDataTableEntity -Context $Context | Select-Object -ExcludeProperty 'Timestamp', 'ETag'
            foreach ($User in $Result) {
                $ExpectedData = Get-ComparableHash ($UsersPSObjects | Where-Object { $_.Id -eq $User.Id })
                Get-ComparableHash $User | Should -Be $ExpectedData
            }
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

        It 'can get count of entities' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            Get-AzDataTableEntity -Context $Context -Count | Should -BeExactly 4
        }

        It 'can remove entities' {
            $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
            Remove-AzDataTableEntity -Context $Context -Entity $UsersToRemovePSObjects | Should -BeNullOrEmpty
            (Get-AzDataTableEntity -Context $Context).Count | Should -BeExactly ($UsersPSObjects.Count - $UsersToRemovePSObjects.Count)
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