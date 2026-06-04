param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("safe", "restricted", "sequence-dry-run")]
    [string]$Lane,

    [string[]]$Stages = @()
)

$repoRoot = Split-Path -Parent $PSScriptRoot
$governanceProject = Join-Path $repoRoot "AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj"

$suiteFilters = @{
    safe = "TestCategory=Suite:SafeOrdered"
    restricted = "TestCategory=Suite:RestrictedOrdered"
}

function Invoke-GovernanceLane([string]$laneName) {
    if ($Stages.Count -gt 0) {
        throw "Stage filters are no longer accepted by the Governance ordered-suite wrapper. Use AioTieba4DotNet.Tests.Governance filters directly when you need narrower execution."
    }

    $filter = $suiteFilters[$laneName]
    Write-Output ("[{0}] Governance ordered suite host -> {1}" -f $laneName, $filter)
    & dotnet test $governanceProject --configuration Release --nologo --filter $filter /p:CollectCoverage=false
    exit $LASTEXITCODE
}

switch ($Lane) {
    "safe" {
        Invoke-GovernanceLane "safe"
    }
    "restricted" {
        Invoke-GovernanceLane "restricted"
    }
    "sequence-dry-run" {
        if ($Stages.Count -gt 0) {
            throw "Stage filters are not supported by the Governance compatibility dry-run wrapper. Inspect ordered suite categories in AioTieba4DotNet.Tests.Governance for stage-level routing."
        }

        Write-Output "1. SafeOrdered [default]"
        Write-Output "    dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --nologo --filter TestCategory=Suite:SafeOrdered /p:CollectCoverage=false"
        Write-Output "2. RestrictedOrdered [explicit opt-in]"
        Write-Output "    dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --nologo --filter TestCategory=Suite:RestrictedOrdered /p:CollectCoverage=false"
        exit 0
    }
}
