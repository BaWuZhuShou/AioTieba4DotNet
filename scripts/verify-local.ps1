param(
    [switch]$ValidateOnly
)

$repoRoot = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $repoRoot ".sisyphus/evidence/local-verification.manifest.json"
$schemaPath = Join-Path $repoRoot ".sisyphus/evidence/local-verification.manifest.schema.json"
$docsInstallCommand = 'pnpm --dir docs install'
$docsBuildCommand = 'pnpm --dir docs run build'

$requiredDocs = @(
    "README.md",
    "docs/index.md",
    "docs/guide/getting-started.md",
    "docs/how-to/forums.md",
    "docs/how-to/threads.md",
    "docs/how-to/users.md",
    "docs/how-to/messages.md",
    "docs/how-to/admins.md",
    "docs/reference/modules.md",
    "docs/related/public-api-coverage-matrix.md",
    "docs/guide/advanced.md",
    "docs/guide/troubleshooting.md",
    "docs/related/migration-v2-to-v3.md",
    "docs/related/release-notes-v3.md",
    "docs/related/parity.md",
    "docs/archive/todo.md",
    "AGENTS.md",
    ".junie/guidelines.md"
)

$archivedDocs = @(
    [ordered]@{
        path = "docs/archive/todo.md"
        requiredPhrases = @(
            "historical archive",
            "docs/related/parity.md",
            "authoritative parity ledger"
        )
    }
)

$legacyRegressionScopes = @(
    [ordered]@{ path = "AioTieba4DotNet"; includes = @("*.cs", "*.csproj") },
    [ordered]@{ path = "AioTieba4DotNet/AGENTS.md"; includes = @() },
    [ordered]@{ path = ".github/workflows/publish.yml"; includes = @() },
    [ordered]@{ path = "README.md"; includes = @() },
    [ordered]@{ path = "docs/related/parity.md"; includes = @() },
    [ordered]@{ path = "docs/archive/todo.md"; includes = @() }
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
        path = "AioTieba4DotNet.Tests.Platform/online-test.safe.template.json"
        requiredBlankKeys = @("safe:account:bduss", "safe:account:stoken")
    },
    [ordered]@{
        path = "AioTieba4DotNet.Tests.Platform/online-test.restricted.template.json"
        requiredBlankKeys = @("restricted:account:bduss", "restricted:account:stoken")
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

$evidenceContentContracts = @()

$truthFreezeEvidenceContract = [ordered]@{
    path = ".sisyphus/evidence/parity-truth-freeze.json"
    repoId = "lumina37/aiotieba"
    canonicalRepoUrl = "https://github.com/lumina37/aiotieba"
    preferredTag = "v4.6.4"
    upstreamSha = "04f8e431f87507a6228b42061c70d298b34317ff"
    comparisonSource = "https://github.com/lumina37/aiotieba/tree/04f8e431f87507a6228b42061c70d298b34317ff"
    sourcePathPolicy = "Authoritative parity truth is the frozen lumina37/aiotieba tuple above. Treat repository-local aiotieba/ as reference-only unless explicit snapshot metadata matches repo id, canonical repo URL, preferred tag, and upstream SHA exactly; missing, mixed, or stale metadata must fail closed."
}

$localEntrypoints = @(
    "scripts/verify-local.ps1",
    "scripts/verify-local.sh",
    "scripts/test-lane.ps1",
    "scripts/test-lane.sh"
)

$requiredEvidence = @(
    [ordered]@{
        id = "parity-truth-freeze"
        kind = "parity-truth-freeze"
        path = $truthFreezeEvidenceContract.path
        description = "Frozen upstream aiotieba truth-source tuple and comparison policy for parity evidence."
    },
    [ordered]@{
        id = "parity-gap-ledger"
        kind = "gap-ledger"
        path = ".sisyphus/evidence/parity-gap-ledger.json"
        description = "Canonical unresolved public parity ledger for the active retained artifact model."
    },
    [ordered]@{
        id = "local-verification-manifest"
        kind = "local-verification-manifest"
        path = ".sisyphus/evidence/local-verification.manifest.json"
        description = "Active local verification manifest for the retained docs, entrypoints, and evidence surface."
    },
    [ordered]@{
        id = "local-verification-manifest-schema"
        kind = "json-schema"
        path = ".sisyphus/evidence/local-verification.manifest.schema.json"
        description = "JSON schema for the active local verification manifest."
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
            'packaging'
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
    $errors.Add('Manifest requiredDocs must match the active VitePress docs contract exactly.')
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

if (Test-NonEmptyFile $truthFreezeEvidenceContract.path) {
    $truthFreezeDocument = Get-Content -Raw (Resolve-RelativePath $truthFreezeEvidenceContract.path) | ConvertFrom-Json -AsHashtable
    foreach ($requiredField in @('repoId', 'canonicalRepoUrl', 'preferredTag', 'upstreamSha', 'comparisonSource', 'sourcePathPolicy', 'generatedAtUtc')) {
        if (-not $truthFreezeDocument.Contains($requiredField)) {
            $errors.Add("Truth-freeze evidence must contain field: $requiredField")
            continue
        }

        if ([string]::IsNullOrWhiteSpace([string]$truthFreezeDocument[$requiredField])) {
            $errors.Add("Truth-freeze evidence field '$requiredField' must be a non-empty string.")
        }
    }

    foreach ($fieldName in @('repoId', 'canonicalRepoUrl', 'preferredTag', 'upstreamSha', 'comparisonSource', 'sourcePathPolicy')) {
        if ($truthFreezeDocument.Contains($fieldName) -and [string]$truthFreezeDocument[$fieldName] -ne [string]$truthFreezeEvidenceContract[$fieldName]) {
            $errors.Add("Truth-freeze evidence field '$fieldName' must equal '$($truthFreezeEvidenceContract[$fieldName])'.")
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
Write-Host "Docs install: $docsInstallCommand"
Write-Host "Docs build:   $docsBuildCommand"
