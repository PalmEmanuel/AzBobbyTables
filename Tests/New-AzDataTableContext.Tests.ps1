param(
    [Parameter()]
    [ValidateScript({ $_ -match '\.psd1$' }, ErrorMessage = 'Please input a .psd1 file')]
    $Manifest = (Get-ChildItem "$PSScriptRoot\..\Source\*.psd1" -Recurse).FullName
)

BeforeAll {
    & "$PSScriptRoot\ModuleReload.ps1" -Manifest $Manifest
}

Describe "New-AzDataTableContext" {
    BeforeAll {
        $Date = Get-Date -Format 'yyyy-MM-dd'
        $FakeTableName = 'faketable'
        $FakeStorageName = 'fakestorage'
        $FakeStorageKey = 'abcdefghijklmnop/qrstuvxyzABCDEFGHIJKLMNOPQRSTUVXYZ1234567+890abcdefghijklmno+pqrstuvxyzA=='
        $FakeConnectionString = "DefaultEndpointsProtocol=https;AccountName=$FakeStorageName;AccountKey=$FakeStorageKey;EndpointSuffix=core.windows.net"
        $FakeSAS = "https://$FakeStorageName.table.core.windows.net/?sv=2021-12-02&ss=bfqt&srt=sco&sp=rwdlacupiytfx&se=${Date}T23:59:59Z&st=${Date}T00:00:00Z&spr=https&sig=abcdefghijklmnopqrstuvxyzABCDEFGHIJKLMNOPQRSTUVXYZ"
        $FakeToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImtpZCI6Imk2bEdrM0ZaenhSY1ViMkMzbkVRN3N5SEpsWSJ9.eyJhdWQiOiI2ZTc0MTcyYi1iZTU2LTQ4NDMtOWZmNC1lNjZhMzliYjEyZTMiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vNzJmOTg4YmYtODZmMS00MWFmLTkxYWItMmQ3Y2QwMTFkYjQ3L3YyLjAiLCJpYXQiOjE1MzcyMzEwNDgsIm5iZiI6MTUzNzIzMTA0OCwiZXhwIjoxNTM3MjM0OTQ4LCJhaW8iOiJBWFFBaS84SUFBQUF0QWFaTG8zQ2hNaWY2S09udHRSQjdlQnE0L0RjY1F6amNKR3hQWXkvQzNqRGFOR3hYZDZ3TklJVkdSZ2hOUm53SjFsT2NBbk5aY2p2a295ckZ4Q3R0djMzMTQwUmlvT0ZKNGJDQ0dWdW9DYWcxdU9UVDIyMjIyZ0h3TFBZUS91Zjc5UVgrMEtJaWpkcm1wNjlSY3R6bVE9PSIsImF6cCI6IjZlNzQxNzJiLWJlNTYtNDg0My05ZmY0LWU2NmEzOWJiMTJlMyIsImF6cGFjciI6IjAiLCJuYW1lIjoiQWJlIExpbmNvbG4iLCJvaWQiOiI2OTAyMjJiZS1mZjFhLTRkNTYtYWJkMS03ZTRmN2QzOGU0NzQiLCJwcmVmZXJyZWRfdXNlcm5hbWUiOiJhYmVsaUBtaWNyb3NvZnQuY29tIiwicmgiOiJJIiwic2NwIjoiYWNjZXNzX2FzX3VzZXIiLCJzdWIiOiJIS1pwZmFIeVdhZGVPb3VZbGl0anJJLUtmZlRtMjIyWDVyclYzeERxZktRIiwidGlkIjoiNzJmOTg4YmYtODZmMS00MWFmLTkxYWItMmQ3Y2QwMTFkYjQ3IiwidXRpIjoiZnFpQnFYTFBqMGVRYTgyUy1JWUZBQSIsInZlciI6IjIuMCJ9.pj4N-w_3Us9DrBLfpCt"
    }

    It "Should return a AzDataTableContext when used with ConnectionString" {
        { New-AzDataTableContext -TableName $FakeTableName -ConnectionString $FakeConnectionString } | Should -Not -Throw
        New-AzDataTableContext -TableName $FakeTableName -ConnectionString $FakeConnectionString | Should -BeOfType 'PipeHow.AzBobbyTables.AzDataTableContext'
    }
    It "Should return a AzDataTableContext when used with SAS" {
        { New-AzDataTableContext -TableName $FakeTableName -SharedAccessSignature $FakeSAS } | Should -Not -Throw
        New-AzDataTableContext -TableName $FakeTableName -SharedAccessSignature $FakeSAS | Should -BeOfType 'PipeHow.AzBobbyTables.AzDataTableContext'
    }
    It "Should return a AzDataTableContext when used with StorageAccountKey" {
        { New-AzDataTableContext -TableName $FakeTableName -StorageAccountName $FakeStorageName -StorageAccountKey $FakeStorageName } | Should -Not -Throw
        New-AzDataTableContext -TableName $FakeTableName -StorageAccountName $FakeStorageName -StorageAccountKey $FakeStorageName | Should -BeOfType 'PipeHow.AzBobbyTables.AzDataTableContext'
    }
    It "Should return a AzDataTableContext when used with Token" {
        { New-AzDataTableContext -TableName $FakeTableName -StorageAccountName $FakeStorageName -Token $FakeToken } | Should -Not -Throw
        New-AzDataTableContext -TableName $FakeTableName -StorageAccountName $FakeStorageName -Token $FakeToken | Should -BeOfType 'PipeHow.AzBobbyTables.AzDataTableContext'
    }
    It "Should return a AzDataTableContext when used with ManagedIdentity" {
        { New-AzDataTableContext -TableName $FakeTableName -StorageAccountName $FakeStorageName -ManagedIdentity } | Should -Not -Throw
        New-AzDataTableContext -TableName $FakeTableName -StorageAccountName $FakeStorageName -ManagedIdentity | Should -BeOfType 'PipeHow.AzBobbyTables.AzDataTableContext'
    }
    It "Should not expose authentication info as properties outside of module assembly" {
        New-AzDataTableContext -TableName $FakeTableName -ConnectionString $FakeConnectionString |
            Get-Member -MemberType Property |
            Select-Object -ExpandProperty Name |
            Should -BeExactly @('ConnectionType','TableName') -Because 'the provided auth info is internal to the module'
    }
}