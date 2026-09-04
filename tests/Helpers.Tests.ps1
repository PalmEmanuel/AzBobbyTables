BeforeAll {
    $BindingFlags = [System.Reflection.BindingFlags]'NonPublic, Static'
    $CreateRequest = [PipeHow.AzBobbyTables.Core.Helpers].GetMethod('CreateManagedIdentityRequest', $BindingFlags)
    $GetArcSecretFilePath = [PipeHow.AzBobbyTables.Core.Helpers].GetMethod('GetArcSecretFilePath', $BindingFlags)
}

Describe 'Managed identity request creation' {
    BeforeEach {
        $OriginalIdentityEndpoint = [Environment]::GetEnvironmentVariable('IDENTITY_ENDPOINT')
        $OriginalIdentityHeader = [Environment]::GetEnvironmentVariable('IDENTITY_HEADER')
    }

    AfterEach {
        [Environment]::SetEnvironmentVariable('IDENTITY_ENDPOINT', $OriginalIdentityEndpoint)
        [Environment]::SetEnvironmentVariable('IDENTITY_HEADER', $OriginalIdentityHeader)
    }

    It 'uses the Azure Arc endpoint and challenge protocol when only IDENTITY_ENDPOINT is set' {
        [Environment]::SetEnvironmentVariable('IDENTITY_ENDPOINT', 'http://127.0.0.1:40342/metadata/identity/oauth2/token')
        [Environment]::SetEnvironmentVariable('IDENTITY_HEADER', $null)
        $Arguments = [object[]]@('https://account.table.core.windows.net', $null, $false)

        $Request = $CreateRequest.Invoke($null, $Arguments)

        $Request.RequestUri.OriginalString | Should -Be 'http://127.0.0.1:40342/metadata/identity/oauth2/token?api-version=2019-11-01&resource=https://account.table.core.windows.net'
        $Request.Headers['Metadata'] | Should -Be 'true'
        $Arguments[2] | Should -BeTrue
    }

    It 'preserves App Service managed identity handling when IDENTITY_HEADER is set' {
        [Environment]::SetEnvironmentVariable('IDENTITY_ENDPOINT', 'http://127.0.0.1/identity')
        [Environment]::SetEnvironmentVariable('IDENTITY_HEADER', 'header-value')
        $Arguments = [object[]]@('https://account.table.core.windows.net', 'client-id', $false)

        $Request = $CreateRequest.Invoke($null, $Arguments)

        $Request.RequestUri.OriginalString | Should -Be 'http://127.0.0.1/identity?api-version=2019-08-01&resource=https://account.table.core.windows.net&client_id=client-id'
        $Request.Headers['X-IDENTITY-HEADER'] | Should -Be 'header-value'
        $Arguments[2] | Should -BeFalse
    }

    It 'preserves the Azure VM IMDS fallback without IDENTITY_ENDPOINT' {
        [Environment]::SetEnvironmentVariable('IDENTITY_ENDPOINT', $null)
        [Environment]::SetEnvironmentVariable('IDENTITY_HEADER', $null)
        $Arguments = [object[]]@('https://account.table.core.windows.net', $null, $false)

        $Request = $CreateRequest.Invoke($null, $Arguments)

        $Request.RequestUri.OriginalString | Should -Be 'http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource=https://account.table.core.windows.net'
        $Request.Headers['Metadata'] | Should -Be 'true'
        $Arguments[2] | Should -BeFalse
    }

    It 'extracts the secret file from the Azure Arc authentication challenge' {
        $GetArcSecretFilePath.Invoke($null, @('Basic realm="/var/opt/azcmagent/tokens/secret.key"')) |
            Should -Be '/var/opt/azcmagent/tokens/secret.key'
    }
}
