BeforeDiscovery {
    # Get command from current test file name
    $Command = Get-Command ((Split-Path $PSCommandPath -Leaf) -replace '.Tests.ps1')
    $ParameterTestCases = @(
        @{
            Command       = $Command
            Name          = 'TableName'
            Type          = 'string'
            ParameterSets = @(
                @{ Name = 'ConnectionString'; Mandatory = $false }
                @{ Name = 'SAS'; Mandatory = $false }
                @{ Name = 'Key'; Mandatory = $false }
                @{ Name = 'Token'; Mandatory = $false }
                @{ Name = 'ManagedIdentity'; Mandatory = $false }
            )
        }
        @{
            Command       = $Command
            Name          = 'ConnectionString'
            Type          = 'string'
            ParameterSets = @(
                @{ Name = 'ConnectionString'; Mandatory = $true }
            )
        }
        @{
            Command       = $Command
            Name          = 'StorageAccountName'
            Type          = 'string'
            ParameterSets = @(
                @{ Name = 'Key'; Mandatory = $true }
                @{ Name = 'Token'; Mandatory = $true }
                @{ Name = 'ManagedIdentity'; Mandatory = $true }
            )
        }
        @{
            Command       = $Command
            Name          = 'StorageAccountKey'
            Type          = 'string'
            ParameterSets = @(
                @{ Name = 'Key'; Mandatory = $true }
            )
        }
        @{
            Command       = $Command
            Name          = 'SharedAccessSignature'
            Type          = 'System.Uri'
            ParameterSets = @(
                @{ Name = 'SAS'; Mandatory = $true }
            )
        }
        @{
            Command       = $Command
            Name          = 'Token'
            Type          = 'string'
            ParameterSets = @(
                @{ Name = 'Token'; Mandatory = $true }
            )
        }
        @{
            Command       = $Command
            Name          = 'ManagedIdentity'
            Type          = 'System.Management.Automation.SwitchParameter'
            ParameterSets = @(
                @{ Name = 'ManagedIdentity'; Mandatory = $true }
            )
        }
        @{
            Command       = $Command
            Name          = 'ClientId'
            Type          = 'string'
            ParameterSets = @(
                @{ Name = 'ManagedIdentity'; Mandatory = $false }
            )
        }
    )
}

Describe 'New-AzDataTableContext' {
    Context 'parameters' {

        It 'only has expected parameters' -TestCases @{ Command = $Command ; Parameters = $ParameterTestCases.Name } {
            $Command.Parameters.GetEnumerator() | Where-Object {
                $_.Key -notin [System.Management.Automation.Cmdlet]::CommonParameters -and
                $_.Key -notin $Parameters
            } | Should -BeNullOrEmpty
        }
        It 'has parameter <Name> of type <Type>' -TestCases $ParameterTestCases {
            $Command | Should -HaveParameter $Name -Type $Type
        }

        It 'has correct parameter sets for parameter <Name>' -TestCases $ParameterTestCases {
            $Parameter = $Command.Parameters[$Name]
            $Parameter.ParameterSets.Keys | Should -BeExactly $ParameterSets.Name
        }

        foreach ($ParameterTestCase in $ParameterTestCases) {
            foreach ($ParameterSet in $ParameterTestCase.ParameterSets) {
                It 'has parameter <ParameterName> set to mandatory <Mandatory> for parameter set <Name>' -TestCases @{
                    Command       = $ParameterTestCase['Command']
                    ParameterName = $ParameterTestCase['Name']
                    Name          = $ParameterSet['Name']
                    Mandatory     = $ParameterSet['Mandatory']
                } {
                    $Parameter = $Command.Parameters[$ParameterName]
                    $Parameter.ParameterSets[$Name].IsMandatory | Should -Be $Mandatory
                }
            }
        }
    }

    Context 'command' {
        BeforeAll {
            $Date = Get-Date -Format 'yyyy-MM-dd'
            $FakeTableName = 'faketable'
            $FakeStorageName = 'fakestorage'
            $FakeStorageKey = 'abcdefghijklmnop/qrstuvxyzABCDEFGHIJKLMNOPQRSTUVXYZ1234567+890abcdefghijklmno+pqrstuvxyzA=='
            $FakeConnectionString = "DefaultEndpointsProtocol=https;AccountName=$FakeStorageName;AccountKey=$FakeStorageKey;EndpointSuffix=core.windows.net"
            $FakeSAS = "https://$FakeStorageName.table.core.windows.net/?sv=2021-12-02&ss=bfqt&srt=sco&sp=rwdlacupiytfx&se=${Date}T23:59:59Z&st=${Date}T00:00:00Z&spr=https&sig=abcdefghijklmnopqrstuvxyzABCDEFGHIJKLMNOPQRSTUVXYZ"
            $FakeToken = 'eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImtpZCI6Imk2bEdrM0ZaenhSY1ViMkMzbkVRN3N5SEpsWSJ9.eyJhdWQiOiI2ZTc0MTcyYi1iZTU2LTQ4NDMtOWZmNC1lNjZhMzliYjEyZTMiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vNzJmOTg4YmYtODZmMS00MWFmLTkxYWItMmQ3Y2QwMTFkYjQ3L3YyLjAiLCJpYXQiOjE1MzcyMzEwNDgsIm5iZiI6MTUzNzIzMTA0OCwiZXhwIjoxNTM3MjM0OTQ4LCJhaW8iOiJBWFFBaS84SUFBQUF0QWFaTG8zQ2hNaWY2S09udHRSQjdlQnE0L0RjY1F6amNKR3hQWXkvQzNqRGFOR3hYZDZ3TklJVkdSZ2hOUm53SjFsT2NBbk5aY2p2a295ckZ4Q3R0djMzMTQwUmlvT0ZKNGJDQ0dWdW9DYWcxdU9UVDIyMjIyZ0h3TFBZUS91Zjc5UVgrMEtJaWpkcm1wNjlSY3R6bVE9PSIsImF6cCI6IjZlNzQxNzJiLWJlNTYtNDg0My05ZmY0LWU2NmEzOWJiMTJlMyIsImF6cGFjciI6IjAiLCJuYW1lIjoiQWJlIExpbmNvbG4iLCJvaWQiOiI2OTAyMjJiZS1mZjFhLTRkNTYtYWJkMS03ZTRmN2QzOGU0NzQiLCJwcmVmZXJyZWRfdXNlcm5hbWUiOiJhYmVsaUBtaWNyb3NvZnQuY29tIiwicmgiOiJJIiwic2NwIjoiYWNjZXNzX2FzX3VzZXIiLCJzdWIiOiJIS1pwZmFIeVdhZGVPb3VZbGl0anJJLUtmZlRtMjIyWDVyclYzeERxZktRIiwidGlkIjoiNzJmOTg4YmYtODZmMS00MWFmLTkxYWItMmQ3Y2QwMTFkYjQ3IiwidXRpIjoiZnFpQnFYTFBqMGVRYTgyUy1JWUZBQSIsInZlciI6IjIuMCJ9.pj4N-w_3Us9DrBLfpCt'
        }
        
        It 'returns a AzDataTableContext when used with ConnectionString' {
            { New-AzDataTableContext -TableName $FakeTableName -ConnectionString $FakeConnectionString } | Should -Not -Throw
            New-AzDataTableContext -TableName $FakeTableName -ConnectionString $FakeConnectionString | Should -BeOfType 'PipeHow.AzBobbyTables.AzDataTableContext'
        }
        
        It 'returns a AzDataTableContext when used with SAS' {
            { New-AzDataTableContext -TableName $FakeTableName -SharedAccessSignature $FakeSAS } | Should -Not -Throw
            New-AzDataTableContext -TableName $FakeTableName -SharedAccessSignature $FakeSAS | Should -BeOfType 'PipeHow.AzBobbyTables.AzDataTableContext'
        }

        It 'returns a AzDataTableContext when used with StorageAccountKey' {
            { New-AzDataTableContext -TableName $FakeTableName -StorageAccountName $FakeStorageName -StorageAccountKey $FakeStorageName } | Should -Not -Throw
            New-AzDataTableContext -TableName $FakeTableName -StorageAccountName $FakeStorageName -StorageAccountKey $FakeStorageName | Should -BeOfType 'PipeHow.AzBobbyTables.AzDataTableContext'
        }

        It 'returns a AzDataTableContext when used with Token' {
            { New-AzDataTableContext -TableName $FakeTableName -StorageAccountName $FakeStorageName -Token $FakeToken } | Should -Not -Throw
            New-AzDataTableContext -TableName $FakeTableName -StorageAccountName $FakeStorageName -Token $FakeToken | Should -BeOfType 'PipeHow.AzBobbyTables.AzDataTableContext'
        }

        It 'returns a AzDataTableContext when used with ManagedIdentity' {
            { New-AzDataTableContext -TableName $FakeTableName -StorageAccountName $FakeStorageName -ManagedIdentity } | Should -Not -Throw
            New-AzDataTableContext -TableName $FakeTableName -StorageAccountName $FakeStorageName -ManagedIdentity | Should -BeOfType 'PipeHow.AzBobbyTables.AzDataTableContext'
        }

        It 'does not expose authentication info as properties outside of module assembly' {
            New-AzDataTableContext -TableName $FakeTableName -ConnectionString $FakeConnectionString |
            Get-Member -MemberType Property |
            Select-Object -ExpandProperty Name |
            Should -BeExactly @('ConnectionType', 'TableName') -Because 'the provided auth info is internal to the module'
        }   
    }
}