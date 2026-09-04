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
    $GetArcSecretFilePath = $Script:HelpersType.GetMethod('GetArcSecretFilePath', $BindingFlags)
    $ReadArcSecret = $Script:HelpersType.GetMethod('ReadArcSecret', $BindingFlags)
}

Describe 'Managed identity request creation' {
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

    It 'preserves App Service managed identity handling when IDENTITY_HEADER is set' {
        [Environment]::SetEnvironmentVariable('IDENTITY_ENDPOINT', 'http://127.0.0.1/identity')
        [Environment]::SetEnvironmentVariable('IDENTITY_HEADER', 'header-value')
        [Environment]::SetEnvironmentVariable('IMDS_ENDPOINT', $null)
        $Arguments = [object[]]@('https://account.table.core.windows.net', 'client-id', $false)

        $Request = $CreateRequest.Invoke($null, $Arguments)

        $Request.RequestUri.OriginalString | Should -Be 'http://127.0.0.1/identity?api-version=2019-08-01&resource=https://account.table.core.windows.net&client_id=client-id'
        $Request.Headers['X-IDENTITY-HEADER'] | Should -Be 'header-value'
        $Arguments[2] | Should -BeFalse
    }

    It 'preserves the Azure VM IMDS fallback without IDENTITY_ENDPOINT' {
        [Environment]::SetEnvironmentVariable('IDENTITY_ENDPOINT', $null)
        [Environment]::SetEnvironmentVariable('IDENTITY_HEADER', $null)
        [Environment]::SetEnvironmentVariable('IMDS_ENDPOINT', $null)
        $Arguments = [object[]]@('https://account.table.core.windows.net', $null, $false)

        $Request = $CreateRequest.Invoke($null, $Arguments)

        $Request.RequestUri.OriginalString | Should -Be 'http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource=https://account.table.core.windows.net'
        $Request.Headers['Metadata'] | Should -Be 'true'
        $Arguments[2] | Should -BeFalse
    }

    It 'does not classify an endpoint without the Azure Arc environment markers as Azure Arc' {
        [Environment]::SetEnvironmentVariable('IDENTITY_ENDPOINT', 'http://127.0.0.1:40342/metadata/identity/oauth2/token')
        [Environment]::SetEnvironmentVariable('IDENTITY_HEADER', $null)
        [Environment]::SetEnvironmentVariable('IMDS_ENDPOINT', $null)
        $Arguments = [object[]]@('https://account.table.core.windows.net', $null, $false)

        $Request = $CreateRequest.Invoke($null, $Arguments)

        $Request.RequestUri.Host | Should -Be '169.254.169.254'
        $Arguments[2] | Should -BeFalse
    }

    It 'extracts the secret file from the Azure Arc authentication challenge' {
        $GetArcSecretFilePath.Invoke($null, @('Basic realm="/var/opt/azcmagent/tokens/secret.key"')) |
            Should -Be '/var/opt/azcmagent/tokens/secret.key'
    }

    It 'rejects Azure Arc challenges for files outside the agent token directory' {
        $ErrorRecord = { $ReadArcSecret.Invoke($null, @('Basic realm="/etc/passwd"')) } |
            Should -Throw -ExceptionType ([System.Management.Automation.MethodInvocationException]) -PassThru
        $ErrorRecord.Exception.InnerException | Should -BeOfType ([System.Net.WebException])
    }

    It 'rejects Azure Arc challenges for secret files that are not .key files' {
        $TokenDirectory = if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
            [System.IO.Path]::Combine([Environment]::GetFolderPath([Environment+SpecialFolder]::CommonApplicationData), 'AzureConnectedMachineAgent', 'Tokens')
        }
        else {
            '/var/opt/azcmagent/tokens'
        }

        if (-not (Test-Path -Path $TokenDirectory)) {
            if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
                New-Item -Path $TokenDirectory -ItemType Directory -Force | Out-Null
            }
            else {
                & sudo mkdir -p $TokenDirectory | Out-Null
            }
        }

        $InvalidSecretPath = [System.IO.Path]::Combine($TokenDirectory, 'not-a-key.txt')
        $ErrorRecord = { $ReadArcSecret.Invoke($null, @("Basic realm=`"$InvalidSecretPath`"")) } |
            Should -Throw -ExceptionType ([System.Management.Automation.MethodInvocationException]) -PassThru
        $ErrorRecord.Exception.InnerException | Should -BeOfType ([System.Net.WebException])
    }

    It 'retries Azure Arc requests using the challenged secret and returns the token' {
        $TokenDirectory = if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
            [System.IO.Path]::Combine([Environment]::GetFolderPath([Environment+SpecialFolder]::CommonApplicationData), 'AzureConnectedMachineAgent', 'Tokens')
        }
        else {
            '/var/opt/azcmagent/tokens'
        }

        if (-not (Test-Path -Path $TokenDirectory)) {
            if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
                New-Item -Path $TokenDirectory -ItemType Directory -Force | Out-Null
            }
            else {
                & sudo mkdir -p $TokenDirectory | Out-Null
            }
        }

        $SecretFile = [System.IO.Path]::Combine($TokenDirectory, 'secret.key')
        $SecretFile = [System.IO.Path]::GetFullPath($SecretFile)
        $SecretFileContents = 'super-secret'
        Set-Content -Path $SecretFile -Value $SecretFileContents -NoNewline

        $Port = 40343
        $ListenerJob = Start-Job -ScriptBlock {
            param($Port, $SecretFile, $SecretFileContents)

            $Listener = [System.Net.HttpListener]::new()
            $Listener.Prefixes.Add("http://127.0.0.1:$Port/")
            $Listener.Start()

            try {
                $FirstRequest = $Listener.GetContext()
                $FirstRequest.Response.StatusCode = 401
                $FirstRequest.Response.AddHeader('WWW-Authenticate', "Basic realm=`"$SecretFile`"")
                $FirstBody = [System.Text.Encoding]::UTF8.GetBytes('Unauthorized')
                $FirstRequest.Response.ContentLength64 = $FirstBody.Length
                $FirstRequest.Response.OutputStream.Write($FirstBody, 0, $FirstBody.Length)
                $FirstRequest.Response.OutputStream.Close()

                $SecondRequest = $Listener.GetContext()
                $SecondRequest.Request.Headers['Authorization'] | Should -Be "Basic $SecretFileContents"
                $SecondRequest.Response.StatusCode = 200
                $SecondRequest.Response.ContentType = 'application/json'
                $SecondBody = [System.Text.Encoding]::UTF8.GetBytes('{"access_token":"token-value"}')
                $SecondRequest.Response.ContentLength64 = $SecondBody.Length
                $SecondRequest.Response.OutputStream.Write($SecondBody, 0, $SecondBody.Length)
                $SecondRequest.Response.OutputStream.Close()
            }
            finally {
                $Listener.Stop()
                $Listener.Close()
            }
        } -ArgumentList $Port, $SecretFile, $SecretFileContents

        try {
            [Environment]::SetEnvironmentVariable('IDENTITY_ENDPOINT', "http://127.0.0.1:$Port/metadata/identity/oauth2/token")
            [Environment]::SetEnvironmentVariable('IDENTITY_HEADER', $null)
            [Environment]::SetEnvironmentVariable('IMDS_ENDPOINT', "http://127.0.0.1:$Port")

            $Token = $Script:HelpersType.GetMethod('GetManagedIdentityToken').Invoke($null, @('account', $null))
            $Token | Should -Be 'token-value'
            $ListenerJob | Wait-Job -Timeout 15 | Out-Null
            $ListenerJob | Receive-Job | Out-Null
        }
        finally {
            Stop-Job -Job $ListenerJob -ErrorAction SilentlyContinue
            Remove-Job -Job $ListenerJob -Force -ErrorAction SilentlyContinue
            if (Test-Path -Path $SecretFile) {
                Remove-Item -Path $SecretFile -Force
            }
        }
    }
}
