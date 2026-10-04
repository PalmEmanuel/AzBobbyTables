BeforeAll {
    Get-AzDataTableSupportedEntityType | Out-Null
    $CoreAssembly = @(
        foreach ($Context in [System.Runtime.Loader.AssemblyLoadContext]::All) {
            foreach ($Assembly in $Context.Assemblies) {
                if ($Assembly.GetName().Name -eq 'AzBobbyTables.Core') { $Assembly }
            }
        }
    )[0]
    $LoadContext = [System.Runtime.Loader.AssemblyLoadContext]::GetLoadContext($CoreAssembly)
    $ServiceType = $CoreAssembly.GetType('PipeHow.AzBobbyTables.Core.AzDataTableService')
    $Flags = [System.Reflection.BindingFlags]'Instance,NonPublic'
    $Script:Constructor = $ServiceType.GetConstructor($Flags, $null, @([System.Threading.CancellationToken]), $null)
    $Script:QueryMethod = $ServiceType.GetMethod('QueryPartRows', $Flags)
    $Script:ClientProperty = $ServiceType.GetProperty('TableClient', $Flags)

    # Capture actual SDK queries: Azurite cannot detect Azure's OR-induced partition scans.
    $ClientSource = @'
using System.Collections.Generic;
using System.Threading;
using Azure;
using Azure.Data.Tables;

public sealed class PartRowLookupClient : TableClient
{
    public List<string> Filters { get; } = new List<string>();
    public List<TableEntity> Rows { get; } = new List<TableEntity>();

    public override Pageable<T> Query<T>(string filter = null, int? maxPerPage = null,
        IEnumerable<string> select = null, CancellationToken cancellationToken = default)
    {
        Filters.Add(filter);
        var rows = new List<T>();
        foreach (var row in Rows) rows.Add((T)(object)row);
        return Pageable<T>.FromPages(new[] { Page<T>.FromValues(rows, null, null) });
    }

    public void AddRow(string rowKey, string owner)
    {
        var row = new TableEntity("p'k", rowKey);
        if (owner != null) row["OriginalEntityId"] = owner;
        Rows.Add(row);
    }
}
'@
    $References = @((Get-ChildItem "$PSHOME/ref/*.dll").FullName) + @(
        $LoadContext.LoadFromAssemblyName([System.Reflection.AssemblyName]::new('Azure.Core')).Location
        $LoadContext.LoadFromAssemblyName([System.Reflection.AssemblyName]::new('Azure.Data.Tables')).Location
    )
    $ClientAssemblyPath = Join-Path $TestDrive 'PartRowLookupClient.dll'
    Add-Type -TypeDefinition $ClientSource -ReferencedAssemblies $References -OutputAssembly $ClientAssemblyPath
    $Script:ClientType = $LoadContext.LoadFromAssemblyPath($ClientAssemblyPath).GetType('PartRowLookupClient')
}

Describe 'Part-row range lookups' {
    BeforeEach {
        $Client = [System.Activator]::CreateInstance($ClientType)
        $Service = $Constructor.Invoke(@([System.Threading.CancellationToken]::None))
        $ClientProperty.SetValue($Service, $Client)
    }

    It 'queries one bounded range per root and only returns rows owned by that root' {
        $Client.AddRow('abc-part1', 'abc')
        $Client.AddRow('abc-partial-part1', 'abc-partial')
        $Client.AddRow('abc-party', $null)
        $Client.AddRow('abc-part2', 'ABC')
        $Client.AddRow('abc-part3', 'neighbour')

        $Rows = @($QueryMethod.Invoke($Service, @("p'k", [string[]]@('abc', 'abc-partial'))))

        $Client.Filters.Count | Should -Be 2
        $Client.Filters[0] | Should -BeExactly "PartitionKey eq 'p''k' and (RowKey ge 'abc-part' and RowKey lt 'abc-paru')"
        $Client.Filters[1] | Should -BeExactly "PartitionKey eq 'p''k' and (RowKey ge 'abc-partial-part' and RowKey lt 'abc-partial-paru')"
        $Rows.Count | Should -Be 2
        $Rows[0].RowKey | Should -BeExactly 'abc-part1'
        $Rows[1].RowKey | Should -BeExactly 'abc-partial-part1'
    }

    It 'escapes quotes in root keys' {
        $null = @($QueryMethod.Invoke($Service, @("p'k", [string[]]@("r'k"))))
        $Client.Filters.Count | Should -Be 1
        $Client.Filters[0] | Should -BeExactly "PartitionKey eq 'p''k' and (RowKey ge 'r''k-part' and RowKey lt 'r''k-paru')"
    }

    It 'does not query when there are no roots' {
        $Rows = @($QueryMethod.Invoke($Service, @("p'k", [string[]]@())))
        $Rows | Should -BeNullOrEmpty
        $Client.Filters.Count | Should -Be 0
    }
}
