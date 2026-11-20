BeforeAll {
    # Query responses are read into pooled buffers. Reached by reflection because AzBobbyTables.Core is
    # loaded into the module's private AssemblyLoadContext.
    Get-AzDataTableSupportedEntityType | Out-Null

    $CoreAssembly = foreach ($Context in [System.Runtime.Loader.AssemblyLoadContext]::All) {
        foreach ($Assembly in $Context.Assemblies) {
            if ($Assembly.GetName().Name -eq 'AzBobbyTables.Core') { $Assembly }
        }
    }
    $Script:ReadFrom = @($CoreAssembly)[0].GetType('PipeHow.AzBobbyTables.Core.PooledBufferStream').GetMethod('ReadFrom')

    function New-Payload {
        param([int]$Length, [int]$Seed)
        $Bytes = [byte[]]::new($Length)
        [System.Random]::new($Seed).NextBytes($Bytes)
        , $Bytes
    }

    function Read-Pooled {
        param([byte[]]$Payload)
        $Script:ReadFrom.Invoke($null, @([System.IO.MemoryStream]::new($Payload)))
    }

    function Get-Content {
        param([System.IO.Stream]$Stream)
        $Copy = [System.IO.MemoryStream]::new()
        $Stream.Position = 0
        $Stream.CopyTo($Copy)
        , $Copy.ToArray()
    }
}

Describe 'Pooled response buffer' {
    It 'returns a <Length> byte response unchanged' -ForEach @(
        @{ Length = 0 }
        @{ Length = 1 }
        @{ Length = 65536 }
        @{ Length = 65537 }
        @{ Length = 3000000 }
        # Larger than the pool keeps: read into a buffer that is not pooled
        @{ Length = 9000000 }
    ) {
        $Payload = New-Payload -Length $Length -Seed $Length
        $Stream = Read-Pooled $Payload
        try {
            $Stream.Length | Should -Be $Length
            [System.Linq.Enumerable]::SequenceEqual([byte[]](Get-Content $Stream), $Payload) | Should -BeTrue
        } finally {
            $Stream.Dispose()
        }
    }

    It 'can be read again after seeking back' {
        $Payload = New-Payload -Length 200000 -Seed 7
        $Stream = Read-Pooled $Payload
        try {
            $null = Get-Content $Stream
            [System.Linq.Enumerable]::SequenceEqual([byte[]](Get-Content $Stream), $Payload) | Should -BeTrue
        } finally {
            $Stream.Dispose()
        }
    }

    It 'refuses reads once disposed' {
        $Stream = Read-Pooled (New-Payload -Length 100 -Seed 1)
        $Stream.Dispose()
        { $Stream.Read([byte[]]::new(10), 0, 10) } | Should -Throw
    }

    It 'never hands one buffer to two readers after a response is disposed twice' {
        $Stream = Read-Pooled (New-Payload -Length 100000 -Seed 1)
        $Stream.Dispose()
        $Stream.Dispose()

        $First = New-Payload -Length 100000 -Seed 2
        $Second = New-Payload -Length 100000 -Seed 3
        $FirstStream = Read-Pooled $First
        $SecondStream = Read-Pooled $Second
        try {
            [System.Linq.Enumerable]::SequenceEqual([byte[]](Get-Content $FirstStream), $First) | Should -BeTrue
            [System.Linq.Enumerable]::SequenceEqual([byte[]](Get-Content $SecondStream), $Second) | Should -BeTrue
        } finally {
            $FirstStream.Dispose()
            $SecondStream.Dispose()
        }
    }
}
