param(
    [switch]$ValidateOnly
)

$repoRoot = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $repoRoot ".sisyphus/evidence/local-verification.manifest.json"
$schemaPath = Join-Path $repoRoot ".sisyphus/evidence/local-verification.manifest.schema.json"

$requiredDocs = @(
    "docs/parity-v3.md",
    "docs/migration-v2-to-v3.md",
    "docs/release-notes-v3.md",
    "README.md",
    "docs/getting-started.md",
    "docs/how-to-forums.md",
    "docs/how-to-threads.md",
    "docs/how-to-users.md",
    "docs/how-to-messages.md",
    "docs/modules.md",
    "docs/advanced.md",
    "docs/troubleshooting.md",
    "docs/todo.md",
    "AGENTS.md",
    ".junie/guidelines.md"
)

$archivedDocs = @(
    [ordered]@{
        path = "docs/todo.md"
        requiredPhrases = @(
            "historical archive",
            "docs/parity-v3.md",
            "authoritative parity ledger"
        )
    }
)

$legacyRegressionScopes = @(
    [ordered]@{ path = "AioTieba4DotNet"; includes = @("*.cs", "*.csproj") },
    [ordered]@{ path = "AioTieba4DotNet/AGENTS.md"; includes = @() },
    [ordered]@{ path = ".github/workflows/publish.yml"; includes = @() },
    [ordered]@{ path = "README.md"; includes = @() },
    [ordered]@{ path = "docs/parity-v3.md"; includes = @() },
    [ordered]@{ path = "docs/todo.md"; includes = @() }
)

$forbiddenLegacyPatterns = @(
    [ordered]@{ name = "ApiBase"; pattern = '(?<!\w)ApiBase(?!\w)' },
    [ordered]@{ name = "ProtoApiBase"; pattern = '(?<!\w)ProtoApiBase(?!\w)' },
    [ordered]@{ name = "ApiWsBase"; pattern = '(?<!\w)ApiWsBase(?!\w)' },
    [ordered]@{ name = "ProtoApiWsBase"; pattern = '(?<!\w)ProtoApiWsBase(?!\w)' },
    [ordered]@{ name = "TiebaRequestMode"; pattern = '(?<!\w)TiebaRequestMode(?!\w)' },
    [ordered]@{ name = "LegacyTransportDispatcher"; pattern = '(?<!\w)LegacyTransportDispatcher(?!\w)' },
    [ordered]@{ name = "LegacyTransportContext"; pattern = '(?<!\w)LegacyTransportContext(?!\w)' },
    [ordered]@{ name = "LegacyForumProtocol"; pattern = '(?<!\w)LegacyForumProtocol(?!\w)' },
    [ordered]@{ name = "LegacyThreadProtocol"; pattern = '(?<!\w)LegacyThreadProtocol(?!\w)' },
    [ordered]@{ name = "LegacyUserProtocol"; pattern = '(?<!\w)LegacyUserProtocol(?!\w)' },
    [ordered]@{ name = "LegacyClientProtocol"; pattern = '(?<!\w)LegacyClientProtocol(?!\w)' }
)

$credentialTemplateFiles = @(
    [ordered]@{
        path = "AioTieba4DotNet.Testing/appsettings.test.json"
        requiredBlankKeys = @("TieBa:BDUSS", "TieBa:STOKEN")
    },
    [ordered]@{
        path = "AioTieba4DotNet.Testing/appsettings.fixtures.example.json"
        requiredBlankKeys = @("TieBa:BDUSS", "TieBa:STOKEN")
    }
)

$workflowContentContracts = @(
    [ordered]@{
        path = ".github/workflows/codeql-analysis.yml"
        requiredPhrases = @(
            'branches: [ "main", "master" ]',
            'dotnet-version: 10.x'
        )
        forbiddenPhrases = @(
            '"v2"',
            '8.x',
            '9.x'
        )
    }
)

$evidenceContentContracts = @(
    [ordered]@{
        path = ".sisyphus/evidence/local-deterministic-verification.md"
        requiredPhrases = @(
            "Command:",
            'pwsh -File ".\scripts\test-lane.ps1" deterministic',
            "Coverage collected:",
            "Lane result: passed",
            "Result:"
        )
        forbiddenPhrases = @("Update this file", "sequence-dry-run")
    },
    [ordered]@{
        path = ".sisyphus/evidence/local-integration-verification.md"
        requiredPhrases = @(
            "Command:",
            'pwsh -File ".\scripts\test-lane.ps1" integration',
            "Observed output:",
            "[integration]",
            "returned exit 0 in this environment",
            "real staged integration-lane execution",
            "Result:"
        )
        forbiddenPhrases = @("Update this file", "sequence-dry-run")
    },
    [ordered]@{
        path = ".sisyphus/evidence/local-live-verification.md"
        requiredPhrases = @(
            "Command:",
            'pwsh -File ".\scripts\test-lane.ps1" sequence-dry-run -Stages ThreadRead,Cleanup',
            "Observed output:",
            "cleanup compensations / recorded object ledger",
            "does not claim that the credentialed live lane itself was executed here",
            "Result:"
        )
        forbiddenPhrases = @("Update this file")
    }
)

$localEntrypoints = @(
    "scripts/verify-local.ps1",
    "scripts/verify-local.sh",
    "scripts/test-lane.ps1",
    "scripts/test-lane.sh"
)

$requiredEvidence = @(
    [ordered]@{
        id = "deterministic-tests-and-coverage"
        kind = "local-verification"
        ownerTask = "18"
        path = ".sisyphus/evidence/local-deterministic-verification.md"
        description = "Record deterministic lane execution and coverage evidence outside GitHub Actions."
    },
    [ordered]@{
        id = "integration-lane"
        kind = "local-verification"
        ownerTask = "19"
        path = ".sisyphus/evidence/local-integration-verification.md"
        description = "Record integration lane execution evidence outside GitHub Actions."
    },
    [ordered]@{
        id = "live-lane"
        kind = "local-verification"
        ownerTask = "19"
        path = ".sisyphus/evidence/local-live-verification.md"
        description = "Record live lane execution and cleanup evidence outside GitHub Actions."
    }
)

function Resolve-RelativePath([string]$relativePath) {
    return Join-Path $repoRoot ($relativePath -replace '/', [IO.Path]::DirectorySeparatorChar)
}

function Test-NonEmptyFile([string]$relativePath) {
    $fullPath = Resolve-RelativePath $relativePath
    return (Test-Path -LiteralPath $fullPath -PathType Leaf) -and ((Get-Item -LiteralPath $fullPath).Length -gt 0)
}

function Get-LegacyScanFiles([string]$relativePath, [string[]]$includes) {
    $fullPath = Resolve-RelativePath $relativePath
    if (-not (Test-Path -LiteralPath $fullPath)) {
        return @()
    }

    $item = Get-Item -LiteralPath $fullPath
    if ($item.PSIsContainer) {
        return Get-ChildItem -LiteralPath $fullPath -Recurse -File -Include $includes
    }

    return @($item)
}

function Get-JsonPathValue([object]$document, [string]$path) {
    $current = $document
    foreach ($segment in $path.Split(':')) {
        if ($null -eq $current) {
            return $null
        }

        if ($current -is [System.Collections.IDictionary]) {
            if (-not $current.Contains($segment)) {
                return $null
            }

            $current = $current[$segment]
            continue
        }

        return $null
    }

    return $current
}

function Get-RepositoryCommit {
    try {
        $commit = & git -C $repoRoot rev-parse HEAD 2>$null
        if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($commit)) {
            return $commit.Trim()
        }
    }
    catch {
    }

    return "UNKNOWN"
}

$expectedManifest = [ordered]@{
    '$schema' = './local-verification.manifest.schema.json'
    schemaVersion = 1
    releaseLine = 'v3'
    generatedAtUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    generatedBy = 'scripts/verify-local.ps1'
    repositoryCommit = Get-RepositoryCommit
    ciPolicy = [ordered]@{
        githubActionsRunsTests = $false
        githubActionsRunsSecretBackedLanes = $false
        releaseGateChecks = @(
            'restore',
            'build',
            'codegen',
            'docs-contract',
            'packaging',
            'evidence-presence'
        )
    }
    requiredDocs = $requiredDocs
    localEntrypoints = $localEntrypoints
    requiredEvidence = $requiredEvidence
}

if (-not $ValidateOnly) {
    $json = $expectedManifest | ConvertTo-Json -Depth 6
    [IO.File]::WriteAllText($manifestPath, $json + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
}

$manifest = Get-Content -Raw $manifestPath | ConvertFrom-Json -AsHashtable
$errors = [System.Collections.Generic.List[string]]::new()

if ($manifest['$schema'] -ne './local-verification.manifest.schema.json') {
    $errors.Add("Manifest must use `$schema './local-verification.manifest.schema.json'.")
}

if ($manifest.schemaVersion -ne 1) {
    $errors.Add('Manifest schemaVersion must be 1.')
}

if ($manifest.releaseLine -ne 'v3') {
    $errors.Add("Manifest releaseLine must be 'v3'.")
}

if ($manifest.generatedBy -notin @('scripts/verify-local.ps1', 'scripts/verify-local.sh')) {
    $errors.Add('Manifest generatedBy must be scripts/verify-local.ps1 or scripts/verify-local.sh.')
}

if ((ConvertTo-Json $manifest.requiredDocs -Depth 4) -ne (ConvertTo-Json $requiredDocs -Depth 4)) {
    $errors.Add('Manifest requiredDocs must match the Task 5 governance doc contract exactly.')
}

if ((ConvertTo-Json $manifest.localEntrypoints -Depth 4) -ne (ConvertTo-Json $localEntrypoints -Depth 4)) {
    $errors.Add('Manifest localEntrypoints must match the expected local verification entrypoints.')
}

if ((ConvertTo-Json $manifest.requiredEvidence -Depth 6) -ne (ConvertTo-Json $requiredEvidence -Depth 6)) {
    $errors.Add('Manifest requiredEvidence must match the expected local evidence contract exactly.')
}

if ($null -eq $manifest.ciPolicy) {
    $errors.Add('Manifest ciPolicy must be an object.')
}
else {
    if ($manifest.ciPolicy.githubActionsRunsTests -ne $false) {
        $errors.Add('Manifest ciPolicy.githubActionsRunsTests must be false.')
    }

    if ($manifest.ciPolicy.githubActionsRunsSecretBackedLanes -ne $false) {
        $errors.Add('Manifest ciPolicy.githubActionsRunsSecretBackedLanes must be false.')
    }

    $expectedChecks = $expectedManifest.ciPolicy.releaseGateChecks
    if ((ConvertTo-Json $manifest.ciPolicy.releaseGateChecks -Depth 4) -ne (ConvertTo-Json $expectedChecks -Depth 4)) {
        $errors.Add('Manifest ciPolicy.releaseGateChecks must match the release governance contract exactly.')
    }
}

$pathsToValidate = @(
    $requiredDocs
    $localEntrypoints
    '.sisyphus/evidence/local-verification.manifest.schema.json'
    '.sisyphus/evidence/local-verification.manifest.json'
    ($requiredEvidence | ForEach-Object { $_.path })
)

foreach ($relativePath in $pathsToValidate) {
    if (-not (Test-NonEmptyFile $relativePath)) {
        $errors.Add("Required contract file is missing or empty: $relativePath")
    }
}

foreach ($archivedDoc in $archivedDocs) {
    $relativePath = $archivedDoc.path
    if (-not (Test-NonEmptyFile $relativePath)) {
        continue
    }

    $content = Get-Content -Raw (Resolve-RelativePath $relativePath)
    foreach ($phrase in $archivedDoc.requiredPhrases) {
        if (-not $content.Contains($phrase)) {
            $errors.Add("Archived document $relativePath must contain phrase: $phrase")
        }
    }
}

foreach ($templateFile in $credentialTemplateFiles) {
    $relativePath = $templateFile.path
    if (-not (Test-NonEmptyFile $relativePath)) {
        continue
    }

    $document = Get-Content -Raw (Resolve-RelativePath $relativePath) | ConvertFrom-Json -AsHashtable
    foreach ($keyPath in $templateFile.requiredBlankKeys) {
        $value = Get-JsonPathValue $document $keyPath
        if (-not [string]::IsNullOrWhiteSpace([string]$value)) {
            $errors.Add("Tracked credential template $relativePath must keep $keyPath blank.")
        }
    }
}

foreach ($workflowContract in $workflowContentContracts) {
    $relativePath = $workflowContract.path
    if (-not (Test-NonEmptyFile $relativePath)) {
        $errors.Add("Required workflow governance file is missing or empty: $relativePath")
        continue
    }

    $content = Get-Content -Raw (Resolve-RelativePath $relativePath)
    foreach ($phrase in $workflowContract.requiredPhrases) {
        if (-not $content.Contains($phrase)) {
            $errors.Add("Workflow governance file $relativePath must contain phrase: $phrase")
        }
    }

    foreach ($phrase in $workflowContract.forbiddenPhrases) {
        if ($content.Contains($phrase)) {
            $errors.Add("Workflow governance file $relativePath must not contain phrase: $phrase")
        }
    }
}

foreach ($evidenceContract in $evidenceContentContracts) {
    $relativePath = $evidenceContract.path
    if (-not (Test-NonEmptyFile $relativePath)) {
        continue
    }

    $content = Get-Content -Raw (Resolve-RelativePath $relativePath)
    foreach ($phrase in $evidenceContract.requiredPhrases) {
        if (-not $content.Contains($phrase)) {
            $errors.Add("Evidence record $relativePath must contain phrase: $phrase")
        }
    }

    foreach ($phrase in $evidenceContract.forbiddenPhrases) {
        if ($content.Contains($phrase)) {
            $errors.Add("Evidence record $relativePath must not contain placeholder phrase: $phrase")
        }
    }
}

foreach ($scope in $legacyRegressionScopes) {
    $files = Get-LegacyScanFiles $scope.path $scope.includes
    foreach ($file in $files) {
        $relativeFile = [IO.Path]::GetRelativePath($repoRoot, $file.FullName).Replace([IO.Path]::DirectorySeparatorChar, '/')
        $content = Get-Content -Raw $file.FullName

        foreach ($pattern in $forbiddenLegacyPatterns) {
            if ([Text.RegularExpressions.Regex]::IsMatch($content, $pattern.pattern)) {
                $errors.Add("Forbidden legacy spine reference '$($pattern.name)' found in $relativeFile")
            }
        }
    }
}

if ($errors.Count -gt 0) {
    Write-Host 'Local verification contract validation failed.'
    foreach ($validationError in $errors) {
        Write-Host "- $validationError"
    }

    exit 1
}

Write-Host 'Local verification contract validation passed.'
Write-Host 'Manifest: .sisyphus/evidence/local-verification.manifest.json'
Write-Host 'Schema:   .sisyphus/evidence/local-verification.manifest.schema.json'
