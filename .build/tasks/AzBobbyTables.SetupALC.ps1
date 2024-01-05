task setupALC {
    $ProjectName = Get-SamplerProjectName -BuildRoot $BuildRoot
    $Path = (Get-Module -ListAvailable "$BuildModuleOutput/$ProjectName" | Sort-Object Version -Descending | Select-Object -First 1).Path

    $ModuleFolder = Split-Path -Path (Resolve-Path -Path $Path) -Parent
    Write-Host $Path
    Write-Host $ModuleFolder

    Push-Location $ModuleFolder

    Rename-Item -Path 'AzBobbyTables.Core' -NewName 'dependencies'
    Get-ChildItem -Path 'AzBobbyTables.PS' -File -Filter AzBobbyTables.PS* | Move-Item -Destination .
    Remove-Item AzBobbyTables.PS

    Pop-Location
}