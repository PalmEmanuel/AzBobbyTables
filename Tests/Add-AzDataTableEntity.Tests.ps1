param(
    [Parameter()]
    [ValidateScript({ $_ -match '\.psd1$' }, ErrorMessage = 'Please input a .psd1 file')]
    $Manifest
)

BeforeDiscovery {
    . "$PSScriptRoot\CommonTestLogic.ps1"
    Invoke-ModuleReload -Manifest $Manifest
}

Describe 'Add-AzDataTableEntity' {
    Context 'parameters' {
        # Get command from current test file name
        $Command = Get-Command ((Split-Path $PSCommandPath -Leaf) -replace '.Tests.ps1')
        $ParameterTestCases = Get-CommonOperationCommandParameterTestCases -Command $Command
        $ParameterTestCases += @(
            @{
                Command       = $Command
                Name          = 'Entity'
                Type          = 'System.Collections.Hashtable[]'
                ParameterSets = @(
                    @{ Name = 'TableOperation'; Mandatory = $true }
                )
            }
            @{
                Command       = $Command
                Name          = 'Force'
                Type          = 'System.Management.Automation.SwitchParameter'
                ParameterSets = @(
                    @{ Name = 'TableOperation'; Mandatory = $false }
                )
            }
        )

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