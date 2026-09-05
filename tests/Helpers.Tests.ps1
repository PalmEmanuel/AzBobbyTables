BeforeAll {
    Get-AzDataTableSupportedEntityType | Out-Null

    $CoreAssembly = foreach ($Context in [System.Runtime.Loader.AssemblyLoadContext]::All) {
        foreach ($Assembly in $Context.Assemblies) {
            if ($Assembly.GetName().Name -eq 'AzBobbyTables.Core') { $Assembly }
        }
    }
    $Script:CoreAssembly = @($CoreAssembly)[0]
    $Script:HelpersType = $Script:CoreAssembly.GetType('PipeHow.AzBobbyTables.Core.Helpers')

    $BindingFlags = [System.Reflection.BindingFlags]'NonPublic, Static'
    $CreateRequest = $Script:HelpersType.GetMethod('CreateManagedIdentityRequest', $BindingFlags)
    $ReadArcSecret = $Script:HelpersType.GetMethod('ReadArcSecret', $BindingFlags)
}

Describe 'Azure Arc managed identity' {
    BeforeEach {
        $OriginalIdentityEndpoint = [Environment]::GetEnvironmentVariable('IDENTITY_ENDPOINT')
        $OriginalIdentityHeader = [Environment]::GetEnvironmentVariable('IDENTITY_HEADER')
        $OriginalImdsEndpoint = [Environment]::GetEnvironmentVariable('IMDS_ENDPOINT')
    }

    AfterEach {
        [Environment]::SetEnvironmentVariable('IDENTITY_ENDPOINT', $OriginalIdentityEndpoint)
        [Environment]::SetEnvironmentVariable('IDENTITY_HEADER', $OriginalIdentityHeader)
        [Environment]::SetEnvironmentVariable('IMDS_ENDPOINT', $OriginalImdsEndpoint)
    }

    It 'uses the Azure Arc endpoint and challenge protocol when both Arc endpoints are set' {
        [Environment]::SetEnvironmentVariable('IDENTITY_ENDPOINT', 'http://127.0.0.1:40342/metadata/identity/oauth2/token')
        [Environment]::SetEnvironmentVariable('IDENTITY_HEADER', $null)
        [Environment]::SetEnvironmentVariable('IMDS_ENDPOINT', 'http://127.0.0.1:40342')
        $Arguments = [object[]]@('https://account.table.core.windows.net', $null, $false)

        $Request = $CreateRequest.Invoke($null, $Arguments)

        $Request.RequestUri.OriginalString | Should -Be 'http://127.0.0.1:40342/metadata/identity/oauth2/token?api-version=2019-11-01&resource=https://account.table.core.windows.net'
        $Request.Headers['Metadata'] | Should -Be 'true'
        $Arguments[2] | Should -BeTrue
    }

    It 'rejects Azure Arc challenges for files outside the agent token directory' {
        $ErrorRecord = { $ReadArcSecret.Invoke($null, @('Basic realm="/etc/passwd"')) } |
            Should -Throw -ExceptionType ([System.Management.Automation.MethodInvocationException]) -PassThru
        $ErrorRecord.Exception.InnerException | Should -BeOfType ([System.Net.WebException])
    }
}
