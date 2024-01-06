task repairPlatyPSHelp {
    # Workaround to run post-build to avoid platyPS generating documentation for common parameter ProgressAction
    . "$BuildRoot\Scripts\PlatyPSWorkaround.ps1"
    Repair-PlatyPSMarkdown -Path (Get-ChildItem "$BuildRoot/$HelpSourceFolder/$HelpOutputFolder") -ParameterName 'ProgressAction'
}