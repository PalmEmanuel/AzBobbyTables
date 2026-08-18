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

        It 'rejects removal of a changed entity without -Force' {
            # ETag validation in RemoveLargeEntitiesFromTable is distinct code from the
            # standard remove path and would silently break if validateEtag were ignored.
            Add-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'removal3'; RowKey = 'etag'; V = 'original' } -Force

            $stale = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'removal3'"
            $fresh = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'removal3'"

            # Mutate via the fresh handle so its ETag advances; $stale is now out of date.
            Add-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'removal3'; RowKey = 'etag'; V = 'updated' } -Force

            { Remove-AzDataTableLargeEntity -Context $Context -Entity $stale -ErrorAction Stop } | Should -Throw
            @(Get-AzDataTableEntity -Context $Context -Filter "PartitionKey eq 'removal3'") | Should -Not -BeNullOrEmpty

            { Remove-AzDataTableLargeEntity -Context $Context -Entity $stale -Force } | Should -Not -Throw
            @(Get-AzDataTableEntity -Context $Context -Filter "PartitionKey eq 'removal3'") | Should -BeNullOrEmpty
        }
    }

    Context 'updating' {
        It 'fails on a missing entity and creates nothing' {
            # An update must fail on a missing entity, never create one the way an
            # upsert racing a concurrent delete would.
            foreach ($operation in @('UpdateMerge', 'UpdateReplace')) {
                { Update-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'update-missing'; RowKey = 'ghost'; Flag = $true } -OperationType $operation -ErrorAction Stop } | Should -Throw
            }
            @(Get-AzDataTableEntity -Context $Context -Filter "PartitionKey eq 'update-missing'") | Should -BeNullOrEmpty -Because 'a failed update must not leave a partial row behind'
        }

        It 'merges a scalar onto a plain entity' {
            Add-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'update-plain'; RowKey = 'one'; A = 'kept' } -Force
            Update-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'update-plain'; RowKey = 'one'; Flag = $true }

            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'update-plain'"
            $result.A | Should -Be 'kept'
            $result.Flag | Should -BeTrue
        }

        It 'merges a scalar onto a split entity without corrupting chunked properties' {
            $data = New-PatternString -Length 100000 -Seed 'UM'
            Add-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'update-split'; RowKey = 'one'; Data = $data; Plain = 'kept' } -Force

            Update-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'update-split'; RowKey = 'one'; Flag = $true }

            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'update-split'"
            ($result.Data -ceq $data) | Should -BeTrue -Because 'a merge must not lose or corrupt the chunked value'
            $result.Plain | Should -Be 'kept'
            $result.Flag | Should -BeTrue
        }

        It 'merges a small value over a previously chunked property' {
            $data = New-PatternString -Length 100000 -Seed 'OW'
            Add-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'update-overwrite'; RowKey = 'one'; Data = $data; Plain = 'kept' } -Force

            Update-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'update-overwrite'; RowKey = 'one'; Data = 'small' }

            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'update-overwrite'"
            $result.Data | Should -Be 'small' -Because 'the merged value must win over the stale chunks'
            $result.Plain | Should -Be 'kept'

            $raw = @(Get-AzDataTableEntity -Context $Context -Filter "PartitionKey eq 'update-overwrite'")
            $raw[0].PSObject.Properties['Data_Part0'] | Should -BeNullOrEmpty -Because 'stale chunk properties must not survive the rewrite'
        }

        It 'merges an oversized value onto a plain entity' {
            Add-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'update-grow'; RowKey = 'one'; A = 'kept' } -Force
            $data = New-PatternString -Length 100000 -Seed 'GR'

            Update-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'update-grow'; RowKey = 'one'; Data = $data }

            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'update-grow'"
            ($result.Data -ceq $data) | Should -BeTrue
            $result.A | Should -Be 'kept' -Because 'growing past the threshold must not drop the stored properties'
        }

        It 'merges onto a multi-row entity preserving properties on other rows' {
            $v1 = @{ PartitionKey = 'update-multirow'; RowKey = 'one' }
            1..30 | ForEach-Object { $v1["P$_"] = New-PatternString -Length 25000 -Seed "MR$_" }
            Add-AzDataTableLargeEntity -Context $Context -Entity $v1 -Force

            Update-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'update-multirow'; RowKey = 'one'; P1 = 'replaced' }

            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'update-multirow'"
            $result.P1 | Should -Be 'replaced'
            ($result.P30 -ceq $v1.P30) | Should -BeTrue -Because 'properties on other part rows must survive the merge'
        }

        It 'replaces a multi-row entity with a small version and removes stale part rows' {
            $big = @{ PartitionKey = 'update-shrink'; RowKey = 'one' }
            1..40 | ForEach-Object { $big["P$_"] = New-PatternString -Length 25000 -Seed "US$_" }
            Add-AzDataTableLargeEntity -Context $Context -Entity $big -Force
            @(Get-AzDataTableEntity -Context $Context -Filter "PartitionKey eq 'update-shrink'" -Property PartitionKey, RowKey).Count | Should -BeGreaterThan 1

            Update-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'update-shrink'; RowKey = 'one'; V = 'tiny' } -OperationType UpdateReplace

            @(Get-AzDataTableEntity -Context $Context -Filter "PartitionKey eq 'update-shrink'" -Property PartitionKey, RowKey).Count | Should -Be 1
            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'update-shrink'"
            $result.V | Should -Be 'tiny'
            $result.PSObject.Properties['P1'] | Should -BeNullOrEmpty -Because 'a replace must not carry stored properties forward'
        }

        It 'rejects an update of a changed entity without -Force' {
            Add-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'update-etag'; RowKey = 'one'; V = 'original' } -Force

            $stale = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'update-etag'"
            Add-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'update-etag'; RowKey = 'one'; V = 'changed' } -Force

            $stale.V = 'stale write'
            { Update-AzDataTableLargeEntity -Context $Context -Entity $stale -ErrorAction Stop } | Should -Throw
            (Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'update-etag'").V | Should -Be 'changed'

            { Update-AzDataTableLargeEntity -Context $Context -Entity $stale -Force } | Should -Not -Throw
            (Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'update-etag'").V | Should -Be 'stale write'
        }

        It 'removes stale chunk columns when a merged value needs fewer chunks' {
            # 100000 chars split into 4 chunks; the replacement needs only 2. A merge
            # that left Data_Part2/3 behind would corrupt the next reassembly.
            Add-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'update-chunkshrink'; RowKey = 'one'; Data = (New-PatternString -Length 100000 -Seed 'C4') } -Force
            $smaller = New-PatternString -Length 40000 -Seed 'C2'

            Update-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'update-chunkshrink'; RowKey = 'one'; Data = $smaller }

            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'update-chunkshrink'"
            ($result.Data -ceq $smaller) | Should -BeTrue

            $raw = @(Get-AzDataTableEntity -Context $Context -Filter "PartitionKey eq 'update-chunkshrink'")
            $raw.Count | Should -Be 1
            $chunkNames = @($raw[0].PSObject.Properties.Name | Where-Object { $_ -match '^Data_Part\d+$' })
            $manifest = $raw[0].SplitOverProps | ConvertFrom-Json
            ($chunkNames | Sort-Object) | Should -Be (@($manifest.SplitHeaders) | Sort-Object) -Because 'every chunk column must be listed in the manifest and vice versa'
            $raw[0].PSObject.Properties['Data_Part2'] | Should -BeNullOrEmpty -Because 'chunks of the larger old value must not survive'
        }

        It 'leaves no residue rows across alternating grow and shrink updates' {
            Add-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'update-rounds'; RowKey = 'one'; Seed = 0 } -Force

            $sizes = @(120000, 900, 1200000, 50000)
            for ($i = 0; $i -lt $sizes.Count; $i++) {
                $value = New-PatternString -Length $sizes[$i] -Seed "R$i"
                Update-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'update-rounds'; RowKey = 'one'; Data = $value; Round = $i }

                $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'update-rounds'"
                ($result.Data -ceq $value) | Should -BeTrue -Because "round $i must read back exactly"
                $result.Round | Should -Be $i
            }

            # The final 50k value fits one row: everything the 1.2M round created must be gone.
            $raw = @(Get-AzDataTableEntity -Context $Context -Filter "PartitionKey eq 'update-rounds'" -Property PartitionKey, RowKey)
            $raw.Count | Should -Be 1 -Because 'alternating grow/shrink must not accumulate part rows'
        }

        It 'does not bleed into entities whose RowKey shares a prefix' {
            # ReadLogicalEntity fetches by RowKey range, which also matches unrelated
            # neighbours like 'abc2' when updating 'abc'; they must be filtered out.
            $abc = @{ PartitionKey = 'update-prefix'; RowKey = 'abc' }
            1..30 | ForEach-Object { $abc["P$_"] = New-PatternString -Length 25000 -Seed "PX$_" }
            Add-AzDataTableLargeEntity -Context $Context -Entity $abc -Force
            Add-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'update-prefix'; RowKey = 'abc2'; V = 'neighbor' } -Force

            { Update-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'update-prefix'; RowKey = 'ab'; X = 1 } -ErrorAction Stop } | Should -Throw -Because 'a prefix of an existing key is still a missing entity'
            Update-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'update-prefix'; RowKey = 'abc2'; Touched = $true }

            $split = @(Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'update-prefix'") | Where-Object RowKey -EQ 'abc'
            ($split.P30 -ceq $abc.P30) | Should -BeTrue
            $split.PSObject.Properties['X'] | Should -BeNullOrEmpty
            $split.PSObject.Properties['Touched'] | Should -BeNullOrEmpty -Because 'updating a neighbour must not touch the split entity'
        }

        It 'deduplicates duplicate keys in one call, last wins, through the split path' {
            Add-AzDataTableLargeEntity -Context $Context -Entity @{ PartitionKey = 'update-dedup'; RowKey = 'one'; Data = 'seed' } -Force
            $first = New-PatternString -Length 90000 -Seed 'D1'
            $second = New-PatternString -Length 90000 -Seed 'D2'

            Update-AzDataTableLargeEntity -Context $Context -Entity @(
                @{ PartitionKey = 'update-dedup'; RowKey = 'one'; Data = $first }
                @{ PartitionKey = 'update-dedup'; RowKey = 'one'; Data = $second }
            )

            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'update-dedup'"
            ($result.Data -ceq $second) | Should -BeTrue
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

    Context 'partial queries' {
        # A caller's filter has no reason to match the `{RowKey}-part{n}` rows an entity
        # was split over, so reading it must not depend on that.
        BeforeAll {
            $PartialData = New-PatternString -Length 1500000 -Seed 'PARTIAL'
            Add-AzDataTableLargeEntity -Context $Context -Entity @{
                PartitionKey = 'partial'; RowKey = 'split'; Data = $PartialData; Plain = 'kept'
            } -Force

            $PartialRows = @(Get-AzDataTableEntity -Context $Context -Filter "PartitionKey eq 'partial'" -Property PartitionKey, RowKey)
        }

        It 'is a fixture that really is split over several rows' {
            # Guards the tests below from passing trivially if the thresholds change.
            $PartialRows.Count | Should -BeGreaterThan 1
        }

        It 'returns the whole entity when queried by its RowKey alone' {
            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'partial' and RowKey eq 'split'"
            @($result).Count | Should -Be 1
            ($result.Data -ceq $PartialData) | Should -BeTrue -Because 'rows the filter did not match must be fetched'
            $result.Plain | Should -Be 'kept'
        }

        It 'leaks no chunk properties or split markers when recovered' {
            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'partial' and RowKey eq 'split'"
            $result.PSObject.Properties['Data_Part0'] | Should -BeNullOrEmpty
            $result.PSObject.Properties['SplitOverProps'] | Should -BeNullOrEmpty
            $result.PSObject.Properties['PartCount'] | Should -BeNullOrEmpty
            $result.PSObject.Properties['PartIndex'] | Should -BeNullOrEmpty
        }

        It 'returns the whole entity when a page boundary falls inside it' {
            # First counts physical rows, so a value below the row count cuts the entity.
            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'partial'" -First 1
            ($result.Data -ceq $PartialData) | Should -BeTrue -Because 'paging must not truncate an entity'
        }

        It 'returns the whole entity when only a tail row matches' {
            $result = Get-AzDataTableLargeEntity -Context $Context -Filter "PartitionKey eq 'partial' and RowKey eq 'split-part1'"
            ($result.Data -ceq $PartialData) | Should -BeTrue
        }

        It 'records on every row how many rows the entity was split over' {
            $raw = @(Get-AzDataTableEntity -Context $Context -Filter "PartitionKey eq 'partial'")
            $raw | ForEach-Object { $_.PartCount | Should -Be $raw.Count }
        }

        It 'puts the manifest on the first row, the one row a reader always has' {
            $root = Get-AzDataTableEntity -Context $Context -Filter "PartitionKey eq 'partial' and RowKey eq 'split'"
            $root.SplitOverProps | Should -Not -BeNullOrEmpty
        }

        It 'skips an unreconstructable entity, reports it, and still returns the rest' {
            # A part row whose master row is gone cannot be recovered, and must not take the intact
            # entities in the same partition with it.
            $orphanTable = "AzBobbyTablesLEOrphan$([guid]::NewGuid().ToString('N').Substring(0, 8))"
            $orphanContext = New-AzDataTableContext -TableName $orphanTable -ConnectionString $ConnectionString
            $null = New-AzDataTable -Context $orphanContext
            try {
                Add-AzDataTableLargeEntity -Context $orphanContext -Entity @{
                    PartitionKey = 'o'; RowKey = 'broken'; Data = (New-PatternString -Length 1500000 -Seed 'ORPHAN')
                } -Force
                Add-AzDataTableLargeEntity -Context $orphanContext -Entity @{ PartitionKey = 'o'; RowKey = 'intact-a'; V = 1 } -Force
                Add-AzDataTableLargeEntity -Context $orphanContext -Entity @{ PartitionKey = 'o'; RowKey = 'intact-b'; V = 2 } -Force

                # Orphan the parts by deleting only the master row.
                Remove-AzDataTableEntity -Context $orphanContext -Entity @{ PartitionKey = 'o'; RowKey = 'broken' }

                $result = @(Get-AzDataTableLargeEntity -Context $orphanContext -Filter "PartitionKey eq 'o'" -ErrorAction SilentlyContinue -ErrorVariable skipped)

                @($result.RowKey | Sort-Object) | Should -Be @('intact-a', 'intact-b')
                @($skipped).Count | Should -Be 1
                $skipped[0].FullyQualifiedErrorId | Should -BeLike 'IncompleteEntity*'
                # Context carries PartitionKey/RowKey so a caller can name the row to remove.
                $skipped[0].TargetObject | Should -Be 'o/broken'
            } finally {
                Remove-AzDataTable -Context $orphanContext
            }
        }

        It 'does not fail the query when an entity is skipped' {
            $orphanTable = "AzBobbyTablesLENonTerm$([guid]::NewGuid().ToString('N').Substring(0, 8))"
            $orphanContext = New-AzDataTableContext -TableName $orphanTable -ConnectionString $ConnectionString
            $null = New-AzDataTable -Context $orphanContext
            try {
                Add-AzDataTableLargeEntity -Context $orphanContext -Entity @{
                    PartitionKey = 'n'; RowKey = 'broken'; Data = (New-PatternString -Length 1500000 -Seed 'NONTERM')
                } -Force
                Remove-AzDataTableEntity -Context $orphanContext -Entity @{ PartitionKey = 'n'; RowKey = 'broken' }

                # The report is non-terminating, so the pipeline continues unless the caller opts
                # into -ErrorAction Stop.
                { Get-AzDataTableLargeEntity -Context $orphanContext -Filter "PartitionKey eq 'n'" -ErrorAction SilentlyContinue } |
                Should -Not -Throw
            } finally {
                Remove-AzDataTable -Context $orphanContext
            }
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
