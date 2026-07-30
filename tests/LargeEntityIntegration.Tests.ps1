Describe 'Large Entity Integration Tests' -Tag 'Integration' {
    BeforeAll {
        function New-PatternString {
            # Deterministic patterned string: position markers break equality on any
            # truncation, reordering or chunk-boundary regression.
            param([int]$Length, [string]$Seed = 'X', [ValidateSet('Ascii', 'Cjk', 'Emoji')][string]$Kind = 'Ascii')
            $unit = switch ($Kind) {
                'Ascii' { "<$Seed>abcdefghijklmnopqrstuvwxyz0123456789" }
                'Cjk' { '漢字テスト日本語中文字符試験' }
                'Emoji' { '🎉🚀🔥💻🌏' }
            }
            $sb = [System.Text.StringBuilder]::new($Length + $unit.Length)
            while ($sb.Length -lt $Length) { $null = $sb.Append($unit) }
            $s = $sb.ToString(0, $Length)
            if ([char]::IsHighSurrogate($s[$s.Length - 1])) { $s = $s.Substring(0, $s.Length - 1) + 'x' }
            return $s
        }

        # Start single Azurite instance for all tests (no-op if one is already running)
        $AzuriteLocation = "$TestDrive/azurite"
        if (Test-Path $AzuriteLocation) {
            Remove-Item $AzuriteLocation -Recurse -Force -ErrorAction SilentlyContinue
        }
        New-Item -Path $AzuriteLocation -ItemType Directory -Force | Out-Null

        $null = Start-ThreadJob {
            & azurite --silent --location $using:AzuriteLocation
        } -Name 'AzuriteLargeEntity'

        # Wait for Azurite to start
        Start-Sleep -Milliseconds 1000

        $ConnectionString = 'UseDevelopmentStorage=true'
        # Unique table name so leftover state from an aborted earlier run cannot leak in
        $TableName = "AzBobbyTablesLE$([guid]::NewGuid().ToString('N').Substring(0, 8))"
        $Context = New-AzDataTableContext -TableName $TableName -ConnectionString $ConnectionString
        $null = New-AzDataTable -Context $Context
    }

    AfterAll {
        Remove-AzDataTable -Context $Context -ErrorAction SilentlyContinue
    }

    Context 'passthrough of small entities' {
        It 'round trips values without adding split markers' {
            $small = @{ PartitionKey = 'small'; RowKey = 'one'; Name = 'Bobby'; Age = 42; Active = $true }
            Add-AzDataTableLargeEntity -Context $Context -Entity $small -Force

            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'small'"
            $result.Name | Should -Be 'Bobby'
            $result.Age | Should -Be 42
            $result.Active | Should -BeTrue

            $raw = Get-AzDataTableEntity -Context $Context -Filter "PartitionKey eq 'small'"
            $raw.PSObject.Properties['SplitOverProps'] | Should -BeNullOrEmpty
            $raw.PSObject.Properties['OriginalEntityId'] | Should -BeNullOrEmpty
        }

        It 'accepts PSObject entities' {
            $entity = [pscustomobject]@{ PartitionKey = 'psobj'; RowKey = 'one'; Data = (New-PatternString -Length 100000 -Seed 'ps') }
            Add-AzDataTableLargeEntity -Context $Context -Entity $entity -Force

            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'psobj'"
            ($result.Data -ceq $entity.Data) | Should -BeTrue -Because 'PSObject input must round trip through splitting'
        }

        It 'unwraps PSObject-wrapped property values from the pipeline' {
            $entities = 1..3 | ForEach-Object { @{ PartitionKey = 'wrapped'; RowKey = "r$_"; N = $_ } }
            Add-AzDataTableLargeEntity -Context $Context -Entity $entities -Force

            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'wrapped'" | Sort-Object RowKey
            $result[0].N | Should -Be 1
        }
    }

    Context 'cross-column splitting' {
        BeforeAll {
            $ColumnData = New-PatternString -Length 100000 -Seed 'COL'
            $ColumnEntity = @{ PartitionKey = 'column'; RowKey = 'one'; Data = $ColumnData; Plain = 'untouched' }
            Add-AzDataTableLargeEntity -Context $Context -Entity $ColumnEntity -Force
        }

        It 'round trips the oversized property' {
            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'column'"
            ($result.Data -ceq $ColumnData) | Should -BeTrue -Because 'reassembled value must equal the original'
            $result.Plain | Should -Be 'untouched'
        }

        It 'removes the split markers from the returned entity' {
            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'column'"
            $result.PSObject.Properties['SplitOverProps'] | Should -BeNullOrEmpty
            $result.PSObject.Properties['Data_Part0'] | Should -BeNullOrEmpty
        }

        It 'stores the documented wire format: one row, chunk properties and a manifest array' {
            $raw = @(Get-AzDataTableEntity -Context $Context -Filter "PartitionKey eq 'column'")
            $raw.Count | Should -Be 1
            $raw[0].RowKey | Should -Be 'one'
            $raw[0].Data_Part0 | Should -Not -BeNullOrEmpty
            $raw[0].SplitOverProps | Should -Not -BeNullOrEmpty

            $manifest = $raw[0].SplitOverProps | ConvertFrom-Json
            $manifest.OriginalHeader | Should -Be 'Data'
            @($manifest.SplitHeaders).Count | Should -BeGreaterThan 1
        }

        It 'splits exactly above the threshold and not at it' {
            # 32256 is EntitySplitter.DefaultMaxPropertyChars; a change to the default
            # must be a conscious decision that updates this test.
            foreach ($case in @(@{ Length = 32256; Split = $false }, @{ Length = 32257; Split = $true })) {
                $pk = "boundary$($case.Length)"
                Add-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = $pk; RowKey = 'x'; D = ('a' * $case.Length) } -Force
                $raw = Get-AzDataTableEntity -Context $Context -Filter "PartitionKey eq '$pk'"
                [bool]$raw.PSObject.Properties['SplitOverProps'] | Should -Be $case.Split
                $read = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq '$pk'"
                $read.D.Length | Should -Be $case.Length
            }
        }
    }

    Context 'cross-row splitting' {
        BeforeAll {
            # 40 x 25k chars = 2M estimated bytes; splits into 3 rows at the default
            # 900k row budget without any single property being oversized.
            $RowValues = @{}
            $RowEntity = @{ PartitionKey = 'rows'; RowKey = 'one' }
            1..40 | ForEach-Object {
                $v = New-PatternString -Length 25000 -Seed "R$_"
                $RowEntity["P$_"] = $v
                $RowValues["P$_"] = $v
            }
            Add-AzDataTableLargeEntity -Context $Context -Entity $RowEntity -Force
        }

        It 'round trips every property and restores the logical RowKey' {
            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'rows'"
            $result.RowKey | Should -Be 'one'
            $allEqual = $true
            foreach ($key in $RowValues.Keys) {
                if ($result.$key -cne $RowValues[$key]) { $allEqual = $false; break }
            }
            $allEqual | Should -BeTrue -Because 'every distributed property must reassemble'
        }

        It 'stores the documented wire format: part rows with markers' {
            $raw = @(Get-AzDataTableEntity -Context $Context -Filter "PartitionKey eq 'rows'")
            $raw.Count | Should -BeGreaterThan 1

            $row0 = $raw | Where-Object RowKey -EQ 'one'
            $row0.OriginalEntityId | Should -Be 'one'
            $row0.PartIndex | Should -Be 0

            $parts = @($raw | Where-Object RowKey -Like 'one-part*')
            $parts.Count | Should -Be ($raw.Count - 1)
            foreach ($part in $parts) {
                $part.OriginalEntityId | Should -Be 'one'
            }
        }
    }

    Context 'combined column and row splitting' {
        It 'round trips multiple oversized properties whose chunks span rows' {
            $values = @{}
            $entity = @{ PartitionKey = 'combined'; RowKey = 'one' }
            foreach ($name in 'Alpha', 'Beta', 'Gamma') {
                $values[$name] = New-PatternString -Length 400000 -Seed $name
                $entity[$name] = $values[$name]
            }
            Add-AzDataTableLargeEntity -Context $Context -Entity $entity -Force

            @(Get-AzDataTableEntity -Context $Context -Filter "PartitionKey eq 'combined'" -Property PartitionKey, RowKey).Count |
                Should -BeGreaterThan 1

            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'combined'"
            foreach ($name in $values.Keys) {
                ($result.$name -ceq $values[$name]) | Should -BeTrue -Because "property $name must reassemble across rows"
            }
        }
    }

    Context 'storage format contract' {
        # These fixtures are written with the plain cmdlet in the documented storage
        # format, pinning it as a contract independent of the writer implementation.

        It 'reads the single-object manifest form' {
            $chunk0 = New-PatternString -Length 30720 -Seed 'PS0'
            $chunk1 = New-PatternString -Length 20000 -Seed 'PS1'
            Add-AzDataTableEntity -Context $Context -Force -Entity @{
                PartitionKey   = 'psformat'; RowKey = 'colsplit'
                Data_Part0     = $chunk0; Data_Part1 = $chunk1
                # Object form (not array), SplitHeaders first: the manifest must parse
                # in either shape and any key order.
                SplitOverProps = '{"SplitHeaders":["Data_Part0","Data_Part1"],"OriginalHeader":"Data"}'
            }

            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'psformat' and RowKey eq 'colsplit'"
            ($result.Data -ceq ($chunk0 + $chunk1)) | Should -BeTrue -Because 'object-form manifests must join in header order'
            $result.PSObject.Properties['Data_Part0'] | Should -BeNullOrEmpty
            $result.PSObject.Properties['SplitOverProps'] | Should -BeNullOrEmpty
        }

        It 'merges hand-built part rows in PartIndex order' {
            Add-AzDataTableEntity -Context $Context -Force -Entity @(
                @{ PartitionKey = 'psformat'; RowKey = 'rowsplit'; OriginalEntityId = 'rowsplit'; PartIndex = 0; A = 'alpha' }
                @{ PartitionKey = 'psformat'; RowKey = 'rowsplit-part1'; OriginalEntityId = 'rowsplit'; PartIndex = 1; B = 'beta' }
                @{ PartitionKey = 'psformat'; RowKey = 'rowsplit-part2'; OriginalEntityId = 'rowsplit'; PartIndex = 2; C = 'gamma' }
            )

            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'psformat' and RowKey ge 'rowsplit' and RowKey lt 'rowsplitz'"
            @($result).Count | Should -Be 1
            $result.RowKey | Should -Be 'rowsplit'
            $result.A | Should -Be 'alpha'
            $result.B | Should -Be 'beta'
            $result.C | Should -Be 'gamma'
            $result.PSObject.Properties['OriginalEntityId'] | Should -BeNullOrEmpty
            $result.PSObject.Properties['PartIndex'] | Should -BeNullOrEmpty
        }

        It 'joins chunks that live on different rows, regardless of row placement' {
            $chunk0 = New-PatternString -Length 30720 -Seed 'XR0'
            $chunk1 = New-PatternString -Length 30720 -Seed 'XR1'
            # Manifest and first chunk on row 0, second chunk two rows away: rows merge
            # first, the manifest join runs after, so placement must not matter.
            Add-AzDataTableEntity -Context $Context -Force -Entity @(
                @{ PartitionKey = 'psformat'; RowKey = 'span'; OriginalEntityId = 'span'; PartIndex = 0
                    Data_Part0 = $chunk0
                    SplitOverProps = '{"SplitHeaders":["Data_Part0","Data_Part1"],"OriginalHeader":"Data"}' }
                @{ PartitionKey = 'psformat'; RowKey = 'span-part1'; OriginalEntityId = 'span'; PartIndex = 1; Filler = 'other' }
                @{ PartitionKey = 'psformat'; RowKey = 'span-part2'; OriginalEntityId = 'span'; PartIndex = 2; Data_Part1 = $chunk1 }
            )

            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'psformat' and RowKey ge 'span' and RowKey lt 'spanz'"
            ($result.Data -ceq ($chunk0 + $chunk1)) | Should -BeTrue -Because 'chunk join must follow the manifest order across rows'
            $result.Filler | Should -Be 'other'
        }

        It 'prefers a plain row over leftover part rows with the same identity' {
            Add-AzDataTableEntity -Context $Context -Force -Entity @(
                @{ PartitionKey = 'psformat'; RowKey = 'stale'; Current = 'fresh' }
                @{ PartitionKey = 'psformat'; RowKey = 'stale-part1'; OriginalEntityId = 'stale'; PartIndex = 1; Old = 'leftover' }
            )

            $result = @(Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'psformat' and RowKey ge 'stale' and RowKey lt 'stalez'")
            $result.Count | Should -Be 1
            $result[0].Current | Should -Be 'fresh'
            $result[0].PSObject.Properties['Old'] | Should -BeNullOrEmpty
        }

        It 'reports a warning and returns the entity when the manifest is malformed' {
            Add-AzDataTableEntity -Context $Context -Force -Entity @{
                PartitionKey = 'psformat'; RowKey = 'badmanifest'; Keep = 'value'; SplitOverProps = 'this is not json'
            }

            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'psformat' and RowKey eq 'badmanifest'" -WarningVariable warnings -WarningAction SilentlyContinue
            $result.Keep | Should -Be 'value'
            $result.PSObject.Properties['SplitOverProps'] | Should -BeNullOrEmpty
            $warnings | Should -Not -BeNullOrEmpty
        }
    }

    Context 'rewriting and shrinking' {
        It 'removes stale part rows when a split entity shrinks to fewer rows' {
            $big = @{ PartitionKey = 'shrink'; RowKey = 'one' }
            1..40 | ForEach-Object { $big["P$_"] = New-PatternString -Length 25000 -Seed "SB$_" }
            Add-AzDataTableLargeEntity -Context $Context -Entity $big -Force
            $rowsBefore = @(Get-AzDataTableEntity -Context $Context -Filter "PartitionKey eq 'shrink'" -Property PartitionKey, RowKey).Count

            $smaller = @{ PartitionKey = 'shrink'; RowKey = 'one' }
            1..20 | ForEach-Object { $smaller["P$_"] = New-PatternString -Length 25000 -Seed "SS$_" }
            Add-AzDataTableLargeEntity -Context $Context -Entity $smaller -Force

            $rowsAfter = @(Get-AzDataTableEntity -Context $Context -Filter "PartitionKey eq 'shrink'" -Property PartitionKey, RowKey).Count
            $rowsAfter | Should -BeLessThan $rowsBefore

            $result = @(Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'shrink'")
            $result.Count | Should -Be 1
            $result[0].PSObject.Properties['P21'] | Should -BeNullOrEmpty -Because 'stale properties must not resurrect'
            ($result[0].P20 -ceq (New-PatternString -Length 25000 -Seed 'SS20')) | Should -BeTrue
        }

        It 'reads correctly after shrinking below the splitting threshold entirely' {
            $big = @{ PartitionKey = 'shrinksmall'; RowKey = 'one' }
            1..40 | ForEach-Object { $big["P$_"] = New-PatternString -Length 25000 -Seed "T$_" }
            Add-AzDataTableLargeEntity -Context $Context -Entity $big -Force

            Add-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'shrinksmall'; RowKey = 'one'; P1 = 'tiny' } -Force

            $result = @(Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'shrinksmall'")
            $result.Count | Should -Be 1
            $result[0].P1 | Should -Be 'tiny'
            $result[0].PSObject.Properties['P2'] | Should -BeNullOrEmpty
        }

        It 'promotes multi-row merges to replacement so redistribution cannot corrupt reads' {
            $v1 = @{ PartitionKey = 'mergepromo'; RowKey = 'one' }
            1..30 | ForEach-Object { $v1["P$_"] = New-PatternString -Length 25000 -Seed "V1$_" }
            Add-AzDataTableLargeEntity -Context $Context -Entity $v1 -Force

            # Fewer properties, different values, still multi-row, written as a merge:
            # the implementation must promote to replace or stale values would linger.
            $v2 = @{ PartitionKey = 'mergepromo'; RowKey = 'one' }
            1..20 | ForEach-Object { $v2["P$_"] = New-PatternString -Length 25000 -Seed "V2$_" }
            Add-AzDataTableLargeEntity -Context $Context -Entity $v2 -OperationType UpsertMerge

            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'mergepromo'"
            ($result.P1 -ceq $v2.P1) | Should -BeTrue -Because 'the new value must win'
            $result.PSObject.Properties['P21'] | Should -BeNullOrEmpty -Because 'dropped properties must not survive a promoted merge'
        }

        It 'keeps merge semantics for entities that do not split' {
            Add-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'merge'; RowKey = 'one'; A = 'one'; B = 'two' } -Force
            Add-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'merge'; RowKey = 'one'; B = 'three' } -OperationType UpsertMerge

            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'merge'"
            $result.A | Should -Be 'one'
            $result.B | Should -Be 'three'
        }
    }

    Context 'encodings' {
        It 'round trips CJK content whose escaped JSON exceeds the naive size estimate' {
            # Regression guard for the size estimator: non-ASCII characters cost 6
            # bytes on the wire (\uXXXX); an estimator counting UTF-16 alone produces
            # rows the service rejects with EntityTooLarge.
            $entity = @{ PartitionKey = 'cjk'; RowKey = 'one' }
            $values = @{}
            1..36 | ForEach-Object {
                $v = New-PatternString -Length 25000 -Kind Cjk
                $entity["P$_"] = $v
                $values["P$_"] = $v
            }
            Add-AzDataTableLargeEntity -Context $Context -Entity $entity -Force

            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'cjk'"
            $allEqual = $true
            foreach ($key in $values.Keys) {
                if ($result.$key -cne $values[$key]) { $allEqual = $false; break }
            }
            $allEqual | Should -BeTrue -Because 'CJK payloads must survive splitting and reassembly'
        }

        It 'round trips surrogate pairs across chunk boundaries' {
            $emoji = New-PatternString -Length 120000 -Kind Emoji
            Add-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'emoji'; RowKey = 'one'; D = $emoji } -Force

            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'emoji'"
            ($result.D -ceq $emoji) | Should -BeTrue -Because 'chunk boundaries must not split surrogate pairs'
        }

        It 'preserves empty and whitespace-only properties' {
            Add-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'empty'; RowKey = 'one'; Empty = ''; Space = '   '; Big = (New-PatternString -Length 40000 -Seed 'e') } -Force
            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'empty'"
            $result.Space | Should -Be '   '
            $result.Big.Length | Should -Be 40000
        }
    }

    Context 'typed properties' {
        It 'preserves property types through the split path' {
            $guid = [guid]::NewGuid()
            $date = [DateTimeOffset]::UtcNow
            $entity = @{
                PartitionKey = 'typed'; RowKey = 'one'
                Giant        = (New-PatternString -Length 200000 -Seed 'ty')
                Int32V       = [int]123; Int64V = [long]9223372036854775000; DoubleV = [double]3.14159
                BoolV        = $true; DateV = $date; GuidV = $guid; BytesV = [byte[]](1..64)
            }
            Add-AzDataTableLargeEntity -Context $Context -Entity $entity -Force

            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'typed'"
            $result.Int32V | Should -Be 123
            $result.Int64V | Should -Be 9223372036854775000
            $result.BoolV | Should -BeTrue
            $result.GuidV | Should -Be $guid
            @($result.BytesV).Count | Should -Be 64
            [math]::Abs(($result.DateV - $date).TotalSeconds) | Should -BeLessThan 1
            ($result.Giant -ceq $entity.Giant) | Should -BeTrue
        }
    }

    Context 'key escaping' {
        It 'handles keys containing single quotes through split, rewrite and cleanup' {
            $entity = @{ PartitionKey = "O'Brien"; RowKey = "it's"; Data = (New-PatternString -Length 90000 -Seed 'q') }
            Add-AzDataTableLargeEntity -Context $Context -Entity $entity -Force
            # Rewriting triggers the stale-part cleanup query, which must escape the keys
            Add-AzDataTableLargeEntity -Context $Context -Entity $entity -Force

            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'O''Brien'"
            $result.Data.Length | Should -Be 90000

            # Removal exercises the part-row lookup filter with the same keys
            Remove-AzDataTableLargeEntity -Context $Context -Entity $result -Force
            @(Get-AzDataTableEntity -Context $Context -Filter "PartitionKey eq 'O''Brien'") | Should -BeNullOrEmpty
        }
    }

    Context 'removal' {
        It 'removes every part row of a split entity' {
            $entity = @{ PartitionKey = 'removal'; RowKey = 'split' }
            1..40 | ForEach-Object { $entity["P$_"] = New-PatternString -Length 25000 -Seed "rm$_" }
            Add-AzDataTableLargeEntity -Context $Context -Entity $entity -Force
            @(Get-AzDataTableEntity -Context $Context -Filter "PartitionKey eq 'removal'" -Property PartitionKey, RowKey).Count | Should -BeGreaterThan 1

            $target = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'removal'"
            Remove-AzDataTableLargeEntity -Context $Context -Entity $target -Force

            @(Get-AzDataTableEntity -Context $Context -Filter "PartitionKey eq 'removal'") | Should -BeNullOrEmpty
        }

        It 'removes entities that were never split' {
            Add-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'removal2'; RowKey = 'plain'; V = 1 } -Force
            $target = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'removal2'"
            Remove-AzDataTableLargeEntity -Context $Context -Entity $target -Force
            @(Get-AzDataTableEntity -Context $Context -Filter "PartitionKey eq 'removal2'") | Should -BeNullOrEmpty
        }
    }

    Context 'bulk behavior' {
        It 'deduplicates entities with the same keys in one call, last wins' {
            Add-AzDataTableLargeEntity -Context $Context -Force -Entity @(
                @{ PartitionKey = 'dedup'; RowKey = 'one'; V = 'first' }
                @{ PartitionKey = 'dedup'; RowKey = 'one'; V = 'second' }
            )
            (Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'dedup'").V | Should -Be 'second'
        }

        It 'writes more than 100 entities across multiple transactions' {
            $bulk = 1..250 | ForEach-Object { @{ PartitionKey = 'bulk'; RowKey = "r$_"; N = $_ } }
            Add-AzDataTableLargeEntity -Context $Context -Entity $bulk -Force
            @(Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'bulk'").Count | Should -Be 250
        }

        It 'handles small and giant entities mixed in one call' {
            $mixed = [System.Collections.Generic.List[object]]::new()
            1..30 | ForEach-Object { $mixed.Add(@{ PartitionKey = 'mixedbulk'; RowKey = "small$_"; N = $_ }) }
            1..2 | ForEach-Object { $mixed.Add(@{ PartitionKey = 'mixedbulk'; RowKey = "giant$_"; D = (New-PatternString -Length 600000 -Seed "g$_") }) }
            Add-AzDataTableLargeEntity -Context $Context -Entity $mixed.ToArray() -Force

            @(Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'mixedbulk'").Count | Should -Be 32
        }
    }

    Context 'physical row accounting' {
        It 'counts physical rows including parts, as documented' {
            # Dedicated table so the count is deterministic
            $countTable = "AzBobbyTablesLECount$([guid]::NewGuid().ToString('N').Substring(0, 8))"
            $countContext = New-AzDataTableContext -TableName $countTable -ConnectionString $ConnectionString
            $null = New-AzDataTable -Context $countContext
            try {
                Add-AzDataTableLargeEntity -Context $countContext -Entity @{ PartitionKey = 'c'; RowKey = 'small'; V = 1 } -Force
                $rowSplit = @{ PartitionKey = 'c'; RowKey = 'big' }
                1..40 | ForEach-Object { $rowSplit["P$_"] = New-PatternString -Length 25000 -Seed "c$_" }
                Add-AzDataTableLargeEntity -Context $countContext -Entity $rowSplit -Force

                $physicalRows = @(Get-AzDataTableEntity -Context $countContext -Property PartitionKey, RowKey).Count
                Get-AzDataTableLargeEntity -Context $countContext -Count | Should -Be $physicalRows
                @(Get-AzDataTableLargeEntity -Context $countContext -Filter "PartitionKey eq 'c'").Count | Should -Be 2
            } finally {
                Remove-AzDataTable -Context $countContext
            }
        }
    }

    Context 'existing cmdlets unaffected' {
        It 'plain Add-AzDataTableEntity still rejects oversized entities' {
            {
                Add-AzDataTableEntity -Context $Context -Force -ErrorAction Stop -Entity @{
                    PartitionKey = 'plainguard'; RowKey = 'big'; D = (New-PatternString -Length 100000 -Seed 'pg')
                }
            } | Should -Throw
        }

        It 'plain Get-AzDataTableEntity does not reassemble split entities' {
            # 'rows' partition was written split by an earlier context
            $raw = @(Get-AzDataTableEntity -Context $Context -Filter "PartitionKey eq 'rows'")
            $raw.Count | Should -BeGreaterThan 1
            ($raw | Where-Object { $_.PSObject.Properties['OriginalEntityId'] }).Count | Should -BeGreaterThan 0
        }
    }
}
