param(
    [Parameter()]
    [ValidateScript({ $_ -match '\.psd1$' }, ErrorMessage = 'Please input a .psd1 file')]
    $Manifest
)

BeforeDiscovery {
    . "$PSScriptRoot\CommonTestLogic.ps1"
    Invoke-ModuleReload -Manifest $Manifest
}

Describe 'Integration Tests' -Tag 'Integration' {
    Context 'Azurite' {
        It 'is installed' {
            { Get-Command 'azurite' -ErrorAction Stop } | Should -Not -Throw
        }
    }
    Context 'module' {
        BeforeAll {
            $null = Start-ThreadJob { & azurite --silent --location $using:TestDrive } -Name 'AzuriteJob'

            [hashtable[]]$Users = 1..4 | ForEach-Object {
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
            [hashtable[]]$UpdatedUsers = 1..4 | ForEach-Object {
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
            [hashtable[]]$UsersToRemove = 1..2 | ForEach-Object {
                @{
                    'PartitionKey' = 'AzBobbyTables'
                    'RowKey'       = "$_"
                }
            }

            function Get-ComparableHash {
                param($InputObject)

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
            
                return [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes(($PropertyHash.Keys | Sort-Object | ForEach-Object {
                    $PropertyHash[$_]
                }) -join ''))
            }
        }

        It 'can create table and add data' {
            $Context = New-AzDataTableContext -TableName 'AzBobbyTables' -ConnectionString 'UseDevelopmentStorage=true'
            Add-AzDataTableEntity -Context $Context -Entity $Users -CreateTableIfNotExists | Should -BeNullOrEmpty
            $Result = Get-AzDataTableEntity -Context $Context | ForEach-Object { [pscustomobject]$_ } | Select-Object -ExcludeProperty 'Timestamp', 'ETag'
            foreach ($User in $Result) {
                $ExpectedData = Get-ComparableHash ([pscustomobject]($Users | Where-Object { $_['Id'] -eq $User.Id }))
                Get-ComparableHash $User | Should -Be $ExpectedData
            }
        }

        It 'can update entities' {
            $Context = New-AzDataTableContext -TableName 'AzBobbyTables' -ConnectionString 'UseDevelopmentStorage=true'
            Update-AzDataTableEntity -Context $Context -Entity $UpdatedUsers | Should -BeNullOrEmpty
            $Result = Get-AzDataTableEntity -Context $Context | ForEach-Object { [pscustomobject]$_ } | Select-Object -ExcludeProperty 'Timestamp', 'ETag'
            foreach ($User in $Result) {
                $ExpectedData = Get-ComparableHash ([pscustomobject]($UpdatedUsers | Where-Object { $_['Id'] -eq $User.Id }))
                Get-ComparableHash $User | Should -Be $ExpectedData
            }
        }

        It 'can remove entities' {
            $Context = New-AzDataTableContext -TableName 'AzBobbyTables' -ConnectionString 'UseDevelopmentStorage=true'
            Remove-AzDataTableEntity -Context $Context -Entity $UsersToRemove | Should -BeNullOrEmpty
            (Get-AzDataTableEntity -Context $Context).Count | Should -BeExactly ($Users.Count - $UsersToRemove.Count)
        }

        It 'can clear table' {
            $Context = New-AzDataTableContext -TableName 'AzBobbyTables' -ConnectionString 'UseDevelopmentStorage=true'
            Get-AzDataTableEntity -Context $Context | Should -Not -BeNullOrEmpty
            Clear-AzDataTable -Context $Context
            Get-AzDataTableEntity -Context $Context | Should -BeNullOrEmpty
        }

        AfterAll {
            $Context = New-AzDataTableContext -TableName 'AzBobbyTables' -ConnectionString 'UseDevelopmentStorage=true'
            Clear-AzDataTable -Context $Context -ErrorAction SilentlyContinue
            Stop-Job -Name 'AzuriteJob'
            Remove-Job -Name 'AzuriteJob'
        }
    }
}