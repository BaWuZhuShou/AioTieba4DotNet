param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("deterministic", "integration", "live", "sequence-dry-run")]
    [string]$Lane,

    [string[]]$Stages = @()
)

$repoRoot = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $repoRoot "AioTieba4DotNet.Testing/test-sequencing.manifest.json"
$deterministicProject = Join-Path $repoRoot "AioTieba4DotNet.Tests.Deterministic/AioTieba4DotNet.Tests.Deterministic.csproj"
$integrationProject = Join-Path $repoRoot "AioTieba4DotNet.Tests.Integration/AioTieba4DotNet.Tests.Integration.csproj"
$liveProject = Join-Path $repoRoot "AioTieba4DotNet.Tests.Live/AioTieba4DotNet.Tests.Live.csproj"

function Get-Manifest() {
    return (Get-Content -Raw $manifestPath | ConvertFrom-Json)
}

function Resolve-RequestedStages([object]$manifest, [string[]]$requestedStages, [string]$laneName = "") {
    $normalizedStages = @(
        $requestedStages |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            ForEach-Object { $_ -split ',' } |
            ForEach-Object { $_.Trim() } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            Select-Object -Unique
    )
    if ($normalizedStages.Count -eq 0) {
        return @()
    }

    $knownStages = @($manifest.stages | ForEach-Object { $_.name })
    $unknownStages = @($normalizedStages | Where-Object { $_ -notin $knownStages })
    if ($unknownStages.Count -gt 0) {
        throw "Unknown stage filter(s): $($unknownStages -join ', ')."
    }

    if ([string]::IsNullOrWhiteSpace($laneName)) {
        return $normalizedStages
    }

    $laneStages = @($manifest.stages | Where-Object { $_.lanes -contains $laneName } | ForEach-Object { $_.name })
    $outOfLaneStages = @($normalizedStages | Where-Object { $_ -notin $laneStages })
    if ($outOfLaneStages.Count -gt 0) {
        throw "Stage filter(s) are not available for lane '$laneName': $($outOfLaneStages -join ', ')."
    }

    return $normalizedStages
}

function Get-StagesForLane([string]$laneName, [string[]]$requestedStages) {
    $manifest = Get-Manifest
    $resolvedStages = Resolve-RequestedStages $manifest $requestedStages $laneName
    $laneStages = @($manifest.stages | Where-Object { $_.lanes -contains $laneName })
    if ($resolvedStages.Count -eq 0) {
        return $laneStages
    }

    return @($laneStages | Where-Object { $_.name -in $resolvedStages })
}

function Invoke-StagedLane([string]$laneName, [string]$projectPath, [string]$laneCategory, [string[]]$requestedStages) {
    $selectedStages = Get-StagesForLane $laneName $requestedStages
    foreach ($stage in $selectedStages) {
        if ($stage.name -eq "Cleanup") {
            Write-Output ("[{0}] {1} -> cleanup compensations / recorded object ledger" -f $laneName, $stage.name)
            continue
        }

        $filter = "TestCategory=$laneCategory&TestCategory=$($stage.name)"
        Write-Output ("[{0}] {1} -> {2}" -f $laneName, $stage.name, $filter)
        & dotnet test $projectPath --configuration Release --nologo --filter $filter /p:CollectCoverage=false
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
    }
}

switch ($Lane) {
    "deterministic" {
        $process = Start-Process -FilePath "dotnet" -ArgumentList @(
            "test",
            $deterministicProject,
            "--configuration",
            "Release",
            "--nologo",
            "/p:CollectCoverage=true"
        ) -Wait -NoNewWindow -PassThru
        exit $process.ExitCode
    }
    "integration" {
        Invoke-StagedLane "integration" $integrationProject "Integration" $Stages
        exit 0
    }
    "live" {
        Invoke-StagedLane "live" $liveProject "Live" $Stages
        exit 0
    }
    "sequence-dry-run" {
        $manifest = Get-Manifest
        $resolvedStages = Resolve-RequestedStages $manifest $Stages
        $selectedStages = if ($resolvedStages.Count -eq 0) {
            @($manifest.stages)
        }
        else {
            @($manifest.stages | Where-Object { $_.name -in $resolvedStages })
        }

        for ($i = 0; $i -lt $selectedStages.Count; $i++) {
            $stage = $selectedStages[$i]
            $lanes = ($stage.lanes -join ", ")
            $execution = @()
            if ($stage.lanes -contains "integration") {
                $execution += "integration => TestCategory=Integration&TestCategory=$($stage.name)"
            }

            if ($stage.lanes -contains "live") {
                if ($stage.name -eq "Cleanup") {
                    $execution += "live => cleanup compensations / recorded object ledger"
                }
                else {
                    $execution += "live => TestCategory=Live&TestCategory=$($stage.name)"
                }
            }

            Write-Output ("{0}. {1} [{2}] - {3}" -f ($i + 1), $stage.name, $lanes, $stage.description)
            foreach ($command in $execution) {
                Write-Output ("    {0}" -f $command)
            }
        }
    }
}
