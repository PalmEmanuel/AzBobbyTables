param(
    [ValidateSet('Debug', 'Release')]
    [string]
    $Configuration = 'Release',

    [string]
    $Version,

    [Switch]
    $Full
)

$ModuleName = 'AzBobbyTables'
$DotNetVersion = 'netstandard2.0'

$ProjectRoot = "$PSScriptRoot"
$ManifestDirectory = "$ProjectRoot/bin/$ModuleName"
$ModuleDirectory = "$ManifestDirectory/$ModuleName"
$HelpDirectory = "$ManifestDirectory/en-US"

if (Test-Path $ManifestDirectory) {
    Remove-Item -Path $ManifestDirectory -Recurse
}
New-Item -Path $ManifestDirectory -ItemType Directory
New-Item -Path $HelpDirectory -ItemType Directory
New-Item -Path $ModuleDirectory -ItemType Directory

if ($Full) {
    dotnet build-server shutdown
    dotnet clean
}
dotnet publish $ModuleName -c $Configuration

$ModuleFiles = [System.Collections.Generic.HashSet[string]]::new()

Get-ChildItem -Path "$ProjectRoot/$ModuleName/bin/$Configuration/$DotNetVersion/publish" |
Where-Object { $_.Extension -in '.dll', '.pdb' } |
ForEach-Object { 
    [void]$ModuleFiles.Add($_.Name); 
    Copy-Item -LiteralPath $_.FullName -Destination $ModuleDirectory 
}

Copy-Item -Path "$ProjectRoot/$ModuleName/bin/$Configuration/$DotNetVersion/$ModuleName.dll-Help.xml" -Destination $HelpDirectory
Copy-Item -Path "$ProjectRoot/$ModuleName/Manifest/$ModuleName.psd1" -Destination $ManifestDirectory
if (-not $PSBoundParameters.ContainsKey('Version')) {
    try {
        $Version = gitversion /showvariable LegacySemVerPadded
    }
    catch {
        $Version = [string]::Empty
    }
}
if($Version) {
    $SemVer, $PreReleaseTag = $Version.Split('-')
    Update-ModuleManifest -Path "$ManifestDirectory/$ModuleName.psd1" -ModuleVersion $SemVer -Prerelease $PreReleaseTag
}

Compress-Archive -Path $ManifestDirectory -DestinationPath "$ProjectRoot/$ModuleName.zip" -Force