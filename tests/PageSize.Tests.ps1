BeforeAll {
    # AzDataTableService.CalculatePageSize decides the page size sent to the service as $top.
    #
    # Asserted directly rather than through a query, on purpose: Azurite does not enforce the 1000
    # page-size limit, so a behavioural test passes against the emulator whether or not the value
    # is clamped. Only real Azure rejects an oversized page size, with 400 InvalidInput rather than
    # capping it. A -First 10000 call shipped and failed in production through exactly that gap,
    # so the value itself is what needs asserting.
    #
    # Reached by reflection because AzBobbyTables.Core is loaded into the module's private
    # AssemblyLoadContext and its types are deliberately not resolvable from the session.
    Get-AzDataTableSupportedEntityType | Out-Null   # forces Core to load

    $CoreAssembly = foreach ($Context in [System.Runtime.Loader.AssemblyLoadContext]::All) {
        foreach ($Assembly in $Context.Assemblies) {
            if ($Assembly.GetName().Name -eq 'AzBobbyTables.Core') { $Assembly }
        }
    }

    $Script:ServiceType = @($CoreAssembly)[0].GetType('PipeHow.AzBobbyTables.Core.AzDataTableService')
    $Script:PageSizeMethod = $Script:ServiceType.GetMethod('CalculatePageSize')
    $Script:Limit = $Script:ServiceType.GetField('MaxEntitiesPerPage').GetValue($null)

    function Get-PageSize {
        param($Top, $Skip, $Sort)
        $Script:PageSizeMethod.Invoke($null, @($Top, $Skip, $Sort))
    }
}

Describe 'Query page size' {
    It 'resolves the service limit' {
        $Script:Limit | Should -BeExactly 1000
    }

    Context 'clamping to the service limit' {

        It 'clamps <Description>' -TestCases @(
            @{ Description = 'a request just over the limit'; Top = 1001; Skip = $null }
            @{ Description = 'a request much above the limit'; Top = 10000; Skip = $null }
            @{ Description = 'a very large request'; Top = [int]::MaxValue; Skip = $null }
            @{ Description = 'a skip and top that only exceed the limit combined'; Top = 600; Skip = 600 }
            @{ Description = 'a skip and top that would overflow Int32 if added'; Top = [int]::MaxValue; Skip = [int]::MaxValue }
        ) {
            Get-PageSize -Top $Top -Skip $Skip -Sort $null | Should -BeExactly $Script:Limit
        }

        It 'never exceeds the limit for any requested size' {
            foreach ($Top in 1, 999, 1000, 1001, 5000, 100000, [int]::MaxValue) {
                $Size = Get-PageSize -Top $Top -Skip $null -Sort $null
                $Size | Should -BeLessOrEqual $Script:Limit
                $Size | Should -BeGreaterThan 0
            }
        }
    }

    Context 'requests at or under the limit pass through' {

        It 'asks for exactly <Top>' -TestCases @(
            @{ Top = 1 }
            @{ Top = 5 }
            @{ Top = 999 }
            @{ Top = 1000 }
        ) {
            Get-PageSize -Top $Top -Skip $null -Sort $null | Should -BeExactly $Top
        }

        It 'covers the skipped entities as well as the requested ones' {
            # Skip is applied client side, so the page has to include what will be discarded.
            Get-PageSize -Top 10 -Skip 90 -Sort $null | Should -BeExactly 100
        }
    }

    Context 'cases that must not bound the query' {

        It 'returns null when <Description>' -TestCases @(
            @{ Description = 'no limit was requested'; Top = $null; Skip = $null }
            @{ Description = 'Top is zero'; Top = 0; Skip = $null }
            @{ Description = 'Top is negative'; Top = -1; Skip = $null }
            @{ Description = 'only Skip was given'; Top = $null; Skip = 50 }
        ) {
            Get-PageSize -Top $Top -Skip $Skip -Sort $null | Should -BeNullOrEmpty
        }

        It 'returns null when sorting, because ordering has to see every entity' {
            Get-PageSize -Top 5 -Skip $null -Sort ([string[]]@('Name')) | Should -BeNullOrEmpty
            Get-PageSize -Top 5 -Skip 10 -Sort ([string[]]@('Name', 'Id')) | Should -BeNullOrEmpty
        }

        It 'still bounds the query when Sort is an empty array' {
            Get-PageSize -Top 5 -Skip $null -Sort ([string[]]@()) | Should -BeExactly 5
        }
    }

    Context 'every read path uses the clamp' {
        # CalculatePageSize being correct is not enough - a read path carrying its own copy of the
        # page-size logic would bypass it. This is how the large-entity path shipped unclamped:
        # it was added as a separate method with an inline duplicate, and a rebase merged cleanly
        # while leaving it broken.

        It 'routes every Query call through the calculated page size' {
            $Source = Get-Content -Raw "$PSScriptRoot/../source/AzBobbyTables.Core/AzDataTableService.cs"
            # Every maxPerPage assignment must come from the shared helper.
            $Assignments = [regex]::Matches($Source, 'maxPerPage\s*=\s*(.+?);')
            $Assignments.Count | Should -BeGreaterThan 0
            foreach ($Match in $Assignments) {
                $Match.Groups[1].Value | Should -BeLike '*CalculatePageSize*'
            }
        }
    }
}
