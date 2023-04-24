[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [string]$CommitMessage,

    [Parameter(Mandatory)]
    [string]$Token,
    
    [Parameter(Mandatory)]
    [string]$Repository
)

$URL = "https://api.github.com/repos/$Repository"

$Headers = @{
    'authorization' = "token $Token"
    'Accept'        = 'application/vnd.github.v3+json'
}

# Get all releases including drafts
$Releases = Invoke-RestMethod "$URL/releases" -Headers $Headers

# Check if a release draft exists
foreach ($Release in $Releases) {
    if ($Release.draft -and $Release.tag_name -eq 'vNext') {
        Write-Verbose "Found draft with id $($Release.id)"
        $ReleaseId = $Release.id
        $ReleaseBody = $Release.body
    }
}

# Parse commit message
$FirstLine, $Rest = $CommitMessage -split '\n', 2 | ForEach-Object -MemberName Trim
$PR = $FirstLine -replace '.*(#\d+).*', '$1'
$ReleaseMessage = $Rest

# Get PR details from commit
$PRNumber = ($PR -split '#')[1]
$PullRequest = Invoke-RestMethod -Uri "$URL/pulls/$PRNumber"
$PRLabels = $PullRequest.labels.name
Write-Verbose 'Found Pull Request'
Write-Verbose "PR Number: $($PullRequest.number)" 
Write-Verbose "PR Label: $PRLabel"
Write-Verbose "PR Author: $($PullRequest.user.login)"

# Commit details
$MergedCommit = [ordered]@{
    prNumber      = $PRNumber
    commitMessage = $ReleaseMessage
    commitAuthor  = $PullRequest.user.login
    mergedDate    = $PullRequest.merged_at
}

# Only process PRs with certain labels
$ValidReleaseLabels = $PRLabels | Where-Object { $_ -in 'bug', 'documentation', 'enhancement' }
if ($ValidReleaseLabels) {
    $PRLabel = $ValidReleaseLabels[0]
    if (-not [string]::IsNullOrWhiteSpace($ReleaseBody)) {
        Write-Verbose 'Updating release draft body'
        $ReleaseBody = $ReleaseBody | ConvertFrom-Json -AsHashtable -Depth 10
        $ReleaseBody[$PRLabel] += $MergedCommit
    }
    else {
        Write-Verbose 'Creating new release draft body'
        $ReleaseBody = @{
            bug           = @()
            documentation = @()
            enhancement   = @()  
        }
        $ReleaseBody[$PRLabel] += $MergedCommit   
    }
    $ReleaseBody = $ReleaseBody | ConvertTo-Json -Depth 10
    
    $Body = @{
        tag_name = 'vNext'
        name     = 'WIP - Next Release'
        body     = $ReleaseBody
        draft    = $true
    } | ConvertTo-Json -Depth 10
    
    if (-not $ReleaseId) {
        $null = Invoke-RestMethod -Method Post -Headers $Headers -Body $Body -Uri  "$URL/releases" -Verbose
        Write-Verbose 'New releasedraft created'
    }
    else {
        $null = Invoke-RestMethod -Method Patch -Headers $Headers -Body $Body -Uri  "$URL/releases/$ReleaseId" -Verbose
        Write-Verbose "Updated release draft with $PR"
    }
}
else {
    Write-Verbose 'No PR label found, or PR label ignored'
}