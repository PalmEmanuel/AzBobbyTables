BeforeAll {
    # Building a TableClient costs a ClientDiagnostics reflection pass, so services created for the same
    # connection, table and retry count must share one client. Reached by reflection because
    # AzBobbyTables.Core is loaded into the module's private AssemblyLoadContext.
    Get-AzDataTableSupportedEntityType | Out-Null

    $CoreAssembly = foreach ($Context in [System.Runtime.Loader.AssemblyLoadContext]::All) {
        foreach ($Assembly in $Context.Assemblies) {
            if ($Assembly.GetName().Name -eq 'AzBobbyTables.Core') { $Assembly }
        }
    }
    $Script:ServiceType = @($CoreAssembly)[0].GetType('PipeHow.AzBobbyTables.Core.AzDataTableService')
    $Script:Flags = [System.Reflection.BindingFlags]'NonPublic, Instance'
    $Script:Connection = 'DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;'

    function Get-Clients {
        param($Table, $Retries = 0)
        $Service = $Script:ServiceType.GetMethod('CreateWithConnectionString').Invoke($null, @($Script:Connection, $Table, $false, [System.Threading.CancellationToken]::None, $Retries))
        [pscustomobject]@{
            Table   = $Script:ServiceType.GetProperty('TableClient', $Script:Flags).GetValue($Service)
            Service = $Script:ServiceType.GetProperty('TableServiceClient', $Script:Flags).GetValue($Service)
        }
    }
}

Describe 'Table client reuse' {
    It 'reuses the clients for the same connection, table and retry count' {
        $First = Get-Clients -Table 'ReuseA'
        $Second = Get-Clients -Table 'ReuseA'
        [object]::ReferenceEquals($First.Table, $Second.Table) | Should -BeTrue
        [object]::ReferenceEquals($First.Service, $Second.Service) | Should -BeTrue
    }

    It 'keeps a separate table client per table' {
        $First = Get-Clients -Table 'ReuseA'
        $Other = Get-Clients -Table 'ReuseB'
        [object]::ReferenceEquals($First.Table, $Other.Table) | Should -BeFalse
        $Other.Table.Name | Should -Be 'ReuseB'
        [object]::ReferenceEquals($First.Service, $Other.Service) | Should -BeTrue
    }

    It 'keeps separate clients per retry count' {
        $NoRetry = Get-Clients -Table 'ReuseA' -Retries 0
        $Retry = Get-Clients -Table 'ReuseA' -Retries 3
        [object]::ReferenceEquals($NoRetry.Table, $Retry.Table) | Should -BeFalse
        [object]::ReferenceEquals($NoRetry.Service, $Retry.Service) | Should -BeFalse
    }
}
