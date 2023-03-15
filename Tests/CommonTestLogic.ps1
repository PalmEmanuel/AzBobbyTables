function Invoke-ModuleReload {
    param ($Manifest)
    
    Remove-Module (Get-Module $Manifest -ListAvailable).Name -Force -ErrorAction SilentlyContinue
    Import-Module $Manifest -Force
}

function Get-CommonOperationCommandParameterTestCases {
    param ($Command)
    
    return @(
        @{
            Command       = $Command
            Name          = 'Context'
            Type          = 'PipeHow.AzBobbyTables.AzDataTableContext'
            ParameterSets = @(
                @{ Name = 'TableOperation'; Mandatory = $true }
                @{ Name = 'Count'; Mandatory = $true }
            )
        }
        @{
            Command       = $Command
            Name          = 'CreateTableIfNotExists'
            Type          = 'System.Management.Automation.SwitchParameter'
            ParameterSets = @(
                @{ Name = 'TableOperation'; Mandatory = $false }
                @{ Name = 'Count'; Mandatory = $false }
            )
        }
    )
}