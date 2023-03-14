param(
    [Parameter()]
    [ValidateScript({ $_ -match '\.psd1$' }, ErrorMessage = 'Please input a .psd1 file')]
    $Manifest = (Get-ChildItem "$PSScriptRoot\..\Source\*.psd1" -Recurse).FullName
)

BeforeDiscovery {
    # Get module name from manifest
    $ModuleName = (Get-Module $Manifest -ListAvailable).Name
    # Remove and import module
    Remove-Module $ModuleName -Force -ErrorAction SilentlyContinue
    Import-Module $Manifest -Force

    # Get exported commands
    $ExportedCommands = (Get-Module $ModuleName).ExportedCommands.Keys
    
    $RootDirectory = "$(Split-Path -Path $Manifest -Parent)\..\..\..\"

    # Set up testcases
    $CommandTestCases = @()
    $ParametersTestCases = @()
    # Get custom parameters of all exported commands
    foreach ($Command in $ExportedCommands) {
        $Parameters = (Get-Command $Command).Parameters.GetEnumerator() | Where-Object {
            $_.Key -notin [System.Management.Automation.Cmdlet]::CommonParameters -and
            $_.Value.Attributes.DontShow -eq $false
        } | Select-Object -ExpandProperty Key

        foreach ($Parameter in $Parameters) {
            $ParametersTestCases += @{
                Command   = $Command
                Parameter = $Parameter
            }
        }

        $CommandTestCases += @{
            Command = $Command
        }
    }
}

Describe "Module $ModuleName" {
    
    # A module should always have exported commands
    Context 'Exported commands' {
        # Tests run on both uncompiled and compiled modules
        It 'Exported commands exist' -TestCases (@{ Count = $CommandTestCases.Count }) {
            param ( $Count )
            $Count | Should -BeGreaterThan 0 -Because 'commands should exist'
        }

        # This test will run on all commands except for New-AzDataTableContext.
        It "Exported command '<Command>' should have parameter Context." -TestCases $CommandTestCases.Where({ $_['Command'] -ne 'New-AzDataTableContext' }) {
            param ( $Command )
            Get-Command $Command | Should -HaveParameter 'Context'
        }
        
        It "Exported command '<Command>' should have its class file in the correct directory." -TestCases $CommandTestCases {
            param ( $Command )
            $CommandFileName = $Command -replace '-'
            
            "$RootDirectory\Source\$ModuleName.PS\Cmdlets\$CommandFileName.cs" | Should -Exist
        }

        It "Exported command '<Command>' should have a test file." -TestCases $CommandTestCases {
            param ( $Command )
            $Command
            
            "$RootDirectory\Tests\$Command.Tests.ps1" | Should -Exist
        }

        It "Exported command '<Command>' should have a markdown help file." -TestCases $CommandTestCases {
            param ( $Command )
            "$RootDirectory\Docs\Help\$Command.md" | Should -Exist
        }

        It "Markdown help file for '<Command>' should contain parameter '<Parameter>'." -TestCases $ParametersTestCases {
            param ( $Command, $Parameter )

            "$RootDirectory\Docs\Help\$Command.md" | Should -FileContentMatch $Parameter
        }
    }
}