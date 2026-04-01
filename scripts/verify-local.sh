#!/usr/bin/env bash

set -euo pipefail

mode="${1:---sync}"

if [[ "$mode" != "--sync" && "$mode" != "--validate-only" ]]; then
    printf 'usage: ./scripts/verify-local.sh [--sync|--validate-only]\n'
    exit 1
fi

repo_root="$(cd -- "$(dirname -- "$0")/.." && pwd)"
manifest_path="$repo_root/.sisyphus/evidence/local-verification.manifest.json"
schema_path="$repo_root/.sisyphus/evidence/local-verification.manifest.schema.json"

python - "$repo_root" "$manifest_path" "$schema_path" "$mode" <<'PY'
import json
import os
import re
import subprocess
import sys
from datetime import datetime, timezone

repo_root, manifest_path, schema_path, mode = sys.argv[1:5]

required_docs = [
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
    ".junie/guidelines.md",
]

archived_docs = [
    {
        "path": "docs/todo.md",
        "requiredPhrases": [
            "historical archive",
            "docs/parity-v3.md",
            "authoritative parity ledger",
        ],
    },
]

legacy_regression_scopes = [
    {"path": "AioTieba4DotNet", "includes": [".cs", ".csproj"]},
    {"path": "AioTieba4DotNet/AGENTS.md", "includes": []},
    {"path": ".github/workflows/publish.yml", "includes": []},
    {"path": "README.md", "includes": []},
    {"path": "docs/parity-v3.md", "includes": []},
    {"path": "docs/todo.md", "includes": []},
]

forbidden_legacy_patterns = [
    ("ApiBase", r"(?<!\w)ApiBase(?!\w)"),
    ("ProtoApiBase", r"(?<!\w)ProtoApiBase(?!\w)"),
    ("ApiWsBase", r"(?<!\w)ApiWsBase(?!\w)"),
    ("ProtoApiWsBase", r"(?<!\w)ProtoApiWsBase(?!\w)"),
    ("TiebaRequestMode", r"(?<!\w)TiebaRequestMode(?!\w)"),
    ("LegacyTransportDispatcher", r"(?<!\w)LegacyTransportDispatcher(?!\w)"),
    ("LegacyTransportContext", r"(?<!\w)LegacyTransportContext(?!\w)"),
    ("LegacyForumProtocol", r"(?<!\w)LegacyForumProtocol(?!\w)"),
    ("LegacyThreadProtocol", r"(?<!\w)LegacyThreadProtocol(?!\w)"),
    ("LegacyUserProtocol", r"(?<!\w)LegacyUserProtocol(?!\w)"),
    ("LegacyClientProtocol", r"(?<!\w)LegacyClientProtocol(?!\w)"),
]

credential_template_files = [
    {
        "path": "AioTieba4DotNet.Testing/appsettings.test.json",
        "requiredBlankKeys": ["TieBa:BDUSS", "TieBa:STOKEN"],
    },
    {
        "path": "AioTieba4DotNet.Testing/appsettings.fixtures.example.json",
        "requiredBlankKeys": ["TieBa:BDUSS", "TieBa:STOKEN"],
    },
]

workflow_content_contracts = [
    {
        "path": ".github/workflows/codeql-analysis.yml",
        "requiredPhrases": [
            'branches: [ "main", "master" ]',
            "dotnet-version: 10.x",
        ],
        "forbiddenPhrases": ['"v2"', "8.x", "9.x"],
    },
]

evidence_content_contracts = [
    {
        "path": ".sisyphus/evidence/local-deterministic-verification.md",
        "requiredPhrases": [
            "Command:",
            "pwsh -File \".\\scripts\\test-lane.ps1\" deterministic",
            "Coverage collected:",
            "Lane result: passed",
            "Result:",
        ],
        "forbiddenPhrases": ["Update this file", "sequence-dry-run"],
    },
    {
        "path": ".sisyphus/evidence/local-integration-verification.md",
        "requiredPhrases": [
            "Command:",
            "pwsh -File \".\\scripts\\test-lane.ps1\" integration",
            "Observed output:",
            "[integration]",
            "returned exit 0 in this environment",
            "real staged integration-lane execution",
            "Result:",
        ],
        "forbiddenPhrases": ["Update this file", "sequence-dry-run"],
    },
    {
        "path": ".sisyphus/evidence/local-live-verification.md",
        "requiredPhrases": [
            "Command:",
            "pwsh -File \".\\scripts\\test-lane.ps1\" sequence-dry-run -Stages ThreadRead,Cleanup",
            "Observed output:",
            "cleanup compensations / recorded object ledger",
            "does not claim that the credentialed live lane itself was executed here",
            "Result:",
        ],
        "forbiddenPhrases": ["Update this file"],
    },
]

local_entrypoints = [
    "scripts/verify-local.ps1",
    "scripts/verify-local.sh",
    "scripts/test-lane.ps1",
    "scripts/test-lane.sh",
]

required_evidence = [
    {
        "id": "deterministic-tests-and-coverage",
        "kind": "local-verification",
        "ownerTask": "18",
        "path": ".sisyphus/evidence/local-deterministic-verification.md",
        "description": "Record deterministic lane execution and coverage evidence outside GitHub Actions.",
    },
    {
        "id": "integration-lane",
        "kind": "local-verification",
        "ownerTask": "19",
        "path": ".sisyphus/evidence/local-integration-verification.md",
        "description": "Record integration lane execution evidence outside GitHub Actions.",
    },
    {
        "id": "live-lane",
        "kind": "local-verification",
        "ownerTask": "19",
        "path": ".sisyphus/evidence/local-live-verification.md",
        "description": "Record live lane execution and cleanup evidence outside GitHub Actions.",
    },
]


def to_full_path(relative_path: str) -> str:
    return os.path.join(repo_root, relative_path.replace("/", os.sep))


def exists_non_empty(relative_path: str) -> bool:
    full_path = to_full_path(relative_path)
    return os.path.isfile(full_path) and os.path.getsize(full_path) > 0


def get_legacy_scan_files(relative_path: str, includes: list[str]) -> list[str]:
    full_path = to_full_path(relative_path)
    if os.path.isdir(full_path):
        results: list[str] = []
        for root, _, files in os.walk(full_path):
            for name in files:
                if includes and not any(name.endswith(extension) for extension in includes):
                    continue
                results.append(os.path.join(root, name))
        return results

    if os.path.isfile(full_path):
        return [full_path]

    return []


def get_json_path_value(document: object, path: str) -> object | None:
    current = document
    for segment in path.split(":"):
        if not isinstance(current, dict) or segment not in current:
            return None
        current = current[segment]
    return current


def get_repository_commit() -> str:
    try:
        return subprocess.check_output(
            ["git", "rev-parse", "HEAD"],
            cwd=repo_root,
            text=True,
        ).strip()
    except Exception:
        return "UNKNOWN"


expected_manifest = {
    "$schema": "./local-verification.manifest.schema.json",
    "schemaVersion": 1,
    "releaseLine": "v3",
    "generatedAtUtc": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
    "generatedBy": "scripts/verify-local.sh",
    "repositoryCommit": get_repository_commit(),
    "ciPolicy": {
        "githubActionsRunsTests": False,
        "githubActionsRunsSecretBackedLanes": False,
        "releaseGateChecks": [
            "restore",
            "build",
            "codegen",
            "docs-contract",
            "packaging",
            "evidence-presence",
        ],
    },
    "requiredDocs": required_docs,
    "localEntrypoints": local_entrypoints,
    "requiredEvidence": required_evidence,
}

if mode == "--sync":
    with open(manifest_path, "w", encoding="utf-8", newline="\n") as handle:
        json.dump(expected_manifest, handle, ensure_ascii=False, indent=2)
        handle.write("\n")

with open(manifest_path, encoding="utf-8") as handle:
    manifest = json.load(handle)

errors: list[str] = []

if manifest.get("$schema") != "./local-verification.manifest.schema.json":
    errors.append("Manifest must use $schema './local-verification.manifest.schema.json'.")

if manifest.get("schemaVersion") != 1:
    errors.append("Manifest schemaVersion must be 1.")

if manifest.get("releaseLine") != "v3":
    errors.append("Manifest releaseLine must be 'v3'.")

if manifest.get("generatedBy") not in {"scripts/verify-local.sh", "scripts/verify-local.ps1"}:
    errors.append("Manifest generatedBy must be scripts/verify-local.sh or scripts/verify-local.ps1.")

if manifest.get("requiredDocs") != required_docs:
    errors.append("Manifest requiredDocs must match the Task 5 governance doc contract exactly.")

if manifest.get("localEntrypoints") != local_entrypoints:
    errors.append("Manifest localEntrypoints must match the expected local verification entrypoints.")

if manifest.get("requiredEvidence") != required_evidence:
    errors.append("Manifest requiredEvidence must match the expected local evidence contract exactly.")

policy = manifest.get("ciPolicy")
if not isinstance(policy, dict):
    errors.append("Manifest ciPolicy must be an object.")
else:
    if policy.get("githubActionsRunsTests") is not False:
        errors.append("Manifest ciPolicy.githubActionsRunsTests must be false.")
    if policy.get("githubActionsRunsSecretBackedLanes") is not False:
        errors.append("Manifest ciPolicy.githubActionsRunsSecretBackedLanes must be false.")
    if policy.get("releaseGateChecks") != expected_manifest["ciPolicy"]["releaseGateChecks"]:
        errors.append("Manifest ciPolicy.releaseGateChecks must match the release governance contract exactly.")

for relative_path in required_docs + local_entrypoints + [
    ".sisyphus/evidence/local-verification.manifest.schema.json",
    ".sisyphus/evidence/local-verification.manifest.json",
    *[item["path"] for item in required_evidence],
]:
    if not exists_non_empty(relative_path):
        errors.append(f"Required contract file is missing or empty: {relative_path}")

for archived_doc in archived_docs:
    relative_path = archived_doc["path"]
    if not exists_non_empty(relative_path):
        continue

    with open(to_full_path(relative_path), encoding="utf-8") as handle:
        content = handle.read()

    for phrase in archived_doc["requiredPhrases"]:
        if phrase not in content:
            errors.append(f"Archived document {relative_path} must contain phrase: {phrase}")

for template_file in credential_template_files:
    relative_path = template_file["path"]
    if not exists_non_empty(relative_path):
        continue

    with open(to_full_path(relative_path), encoding="utf-8") as handle:
        document = json.load(handle)

    for key_path in template_file["requiredBlankKeys"]:
        value = get_json_path_value(document, key_path)
        if isinstance(value, str) and value.strip():
            errors.append(f"Tracked credential template {relative_path} must keep {key_path} blank.")

for workflow_contract in workflow_content_contracts:
    relative_path = workflow_contract["path"]
    if not exists_non_empty(relative_path):
        errors.append(f"Required workflow governance file is missing or empty: {relative_path}")
        continue

    with open(to_full_path(relative_path), encoding="utf-8") as handle:
        content = handle.read()

    for phrase in workflow_contract["requiredPhrases"]:
        if phrase not in content:
            errors.append(f"Workflow governance file {relative_path} must contain phrase: {phrase}")

    for phrase in workflow_contract["forbiddenPhrases"]:
        if phrase in content:
            errors.append(f"Workflow governance file {relative_path} must not contain phrase: {phrase}")

for evidence_contract in evidence_content_contracts:
    relative_path = evidence_contract["path"]
    if not exists_non_empty(relative_path):
        continue

    with open(to_full_path(relative_path), encoding="utf-8") as handle:
        content = handle.read()

    for phrase in evidence_contract["requiredPhrases"]:
        if phrase not in content:
            errors.append(f"Evidence record {relative_path} must contain phrase: {phrase}")

    for phrase in evidence_contract["forbiddenPhrases"]:
        if phrase in content:
            errors.append(f"Evidence record {relative_path} must not contain placeholder phrase: {phrase}")

for scope in legacy_regression_scopes:
    for file_path in get_legacy_scan_files(scope["path"], scope["includes"]):
        with open(file_path, encoding="utf-8") as handle:
            content = handle.read()

        relative_path = os.path.relpath(file_path, repo_root).replace(os.sep, "/")
        for name, pattern in forbidden_legacy_patterns:
            if re.search(pattern, content):
                errors.append(f"Forbidden legacy spine reference '{name}' found in {relative_path}")

if errors:
    print("Local verification contract validation failed.")
    for error in errors:
        print(f"- {error}")
    sys.exit(1)

print("Local verification contract validation passed.")
print(f"Manifest: {os.path.relpath(manifest_path, repo_root).replace(os.sep, '/')}")
print(f"Schema:   {os.path.relpath(schema_path, repo_root).replace(os.sep, '/')}")
PY
