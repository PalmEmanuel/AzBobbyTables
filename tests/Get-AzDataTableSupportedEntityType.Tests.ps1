BeforeDiscovery {
    # Get command from current test file name
    $script:Command = Get-Command ((Split-Path $PSCommandPath -Leaf) -replace '.Tests.ps1')
}

Describe 'Get-AzDataTableSupportedEntityType' {
    Context 'functionality' {
        BeforeAll {
            $script:Result = Get-AzDataTableSupportedEntityType
        }

        It 'should return output' {
            $script:Result | Should -Not -BeNullOrEmpty
        }

        It 'should return at least one supported type' {
            $script:Result.Count | Should -BeGreaterThan 0
        }

        It 'should return only string values' {
            foreach ($item in $script:Result) {
                $item | Should -BeOfType [System.String]
            }
        }

        It 'should include expected supported types' {
            # Based on the EntityConverterRegistry, these are the built-in supported types
            $ExpectedTypes = @('Hashtable', 'PSObject', 'SortedList')
            
            foreach ($expectedType in $ExpectedTypes) {
                $script:Result | Should -Contain $expectedType -Because "The cmdlet should return '$expectedType' as a supported entity type"
            }
        }

        It 'should return exactly the expected number of supported types' {
            # Based on current implementation, there should be exactly 3 built-in types
            $script:Result.Count | Should -BeExactly 3 -Because "There should be exactly 3 built-in entity converters (Hashtable, PSObject, SortedList)"
        }

        It 'should return types in a consistent order' {
            # Call the cmdlet multiple times to ensure consistent ordering
            $Result1 = Get-AzDataTableSupportedEntityType
            $Result2 = Get-AzDataTableSupportedEntityType
            
            # Compare arrays element by element
            for ($i = 0; $i -lt $Result1.Count; $i++) {
                $Result1[$i] | Should -BeExactly $Result2[$i] -Because "The order of supported types should be consistent across calls"
            }
        }

        It 'should not return null or empty strings' {
            foreach ($item in $script:Result) {
                $item | Should -Not -BeNullOrEmpty -Because "All returned type names should be valid non-empty strings"
            }
        }

        It 'should not return duplicate types' {
            $UniqueTypes = $script:Result | Sort-Object | Get-Unique
            $UniqueTypes.Count | Should -BeExactly $script:Result.Count -Because "There should be no duplicate type names in the result"
        }
    }
}
