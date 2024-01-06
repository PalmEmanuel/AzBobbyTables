BeforeDiscovery {
    # Get command from current test file name
    $Command = Get-Command ((Split-Path $PSCommandPath -Leaf) -replace '.Tests.ps1')
    $ParameterTestCases = @(
        @{
            Command       = $Command
            Name          = 'Context'
            Type          = 'PipeHow.AzBobbyTables.AzDataTableContext'
            ParameterSets = @(
                @{ Name = '__AllParameterSets'; Mandatory = $true }
            )
        }
    )
}

Describe 'Clear-AzDataTable' {
    Context 'parameters' {

        It 'only has expected parameters' -TestCases @{ Command = $Command ; Parameters = $ParameterTestCases.Name } {
            $Command.Parameters.GetEnumerator() | Where-Object {
                $_.Key -notin [System.Management.Automation.Cmdlet]::CommonParameters -and
                $_.Key -notin $Parameters
            } | Should -BeNullOrEmpty
        }

        It 'has parameter <Name> of type <Type>' -TestCases $ParameterTestCases {
            $Command | Should -HaveParameter $Name -Type $Type
        }

        It 'has correct parameter sets for parameter <Name>' -TestCases $ParameterTestCases {
            $Parameter = $Command.Parameters[$Name]
            $Parameter.ParameterSets.Keys | Should -BeExactly $ParameterSets.Name
        }

        foreach ($ParameterTestCase in $ParameterTestCases) {
            foreach ($ParameterSet in $ParameterTestCase.ParameterSets) {
                It 'has parameter <ParameterName> set to mandatory <Mandatory> for parameter set <Name>' -TestCases @{
                    Command       = $ParameterTestCase['Command']
                    ParameterName = $ParameterTestCase['Name']
                    Name          = $ParameterSet['Name']
                    Mandatory     = $ParameterSet['Mandatory']
                } {
                    $Parameter = $Command.Parameters[$ParameterName]
                    $Parameter.ParameterSets[$Name].IsMandatory | Should -Be $Mandatory
                }
            }
        }
    }
}