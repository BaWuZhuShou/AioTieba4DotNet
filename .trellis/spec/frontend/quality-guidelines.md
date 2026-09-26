# Documentation Quality Guidelines

## Choose the Check by What Changed

[docs/package.json](../../../docs/package.json) provides site build/serve scripts. [verify-local.sh](../../../scripts/verify-local.sh) and [verify-local.ps1](../../../scripts/verify-local.ps1) validate repository documentation/evidence contracts. They are separate checks and do not prove live API behavior.

Run commands from the repository root:

| Intent | Command | Prerequisites and scope |
| --- | --- | --- |
| Install site dependencies | `pnpm --dir docs install` | Node and declared pnpm version; package access or cache. Writes dependencies and can update the lockfile. |
| Build the site | `pnpm --dir docs run build` | Installed docs dependencies; checks site generation and configured link checks. |
| Inspect edits locally | `pnpm --dir docs run dev` | Installed dependencies; starts a development server. |
| Inspect built output | `pnpm --dir docs run preview` | Successful site build; starts a preview server. |
| Validate existing contracts with Bash | `bash ./scripts/verify-local.sh --validate-only` | Bash, `python` on PATH, and required local evidence files. |
| Validate existing contracts with PowerShell | `pwsh ./scripts/verify-local.ps1 -ValidateOnly` | PowerShell and required local evidence files. |

The verifier prints the docs install/build commands after checking contracts; it does **not** execute them. Without the validation-only option, it writes the manifest before validation. Choose validation-only when reviewing existing artifacts, and never describe that check as a site build.

For Trellis-spec-only edits, inspect source references, local links, index completeness, template residue, and whitespace. These edits are outside the site content root and do not themselves require dependency installation or live tests.

## Local Evidence Prerequisites

Both verifiers and [ParityArtifactRetentionContract.cs](../../../AioTieba4DotNet.Tests.Governance/Contracts/ParityArtifactRetentionContract.cs) require this tuple under `.sisyphus/evidence/`:

- `parity-truth-freeze.json`
- `parity-gap-ledger.json`
- `local-verification.manifest.json`
- `local-verification.manifest.schema.json`

This directory is ignored by [.gitignore](../../../.gitignore), so a checkout may lack the required artifacts. All four were absent during this bootstrap. If unavailable, report the prerequisite gap; do not invent evidence or remove the contract to obtain a pass. Manifest synchronization alone does not recreate the upstream evidence or schema.

Adding/removing a required guide must keep `required_docs` in the Bash verifier, `$requiredDocs` in the PowerShell verifier, the manifest, and affected local contract tests aligned. [ParityArtifactRetentionContractTests.cs](../../../AioTieba4DotNet.Tests.Governance/Contracts/ParityArtifactRetentionContractTests.cs) demonstrates retained-artifact checks.

## Review Content and Rendered Behavior

- Check examples and signatures against public declarations, including numeric ID types, optional parameters, module ownership, and disposal.
- Check navigation, home-page destinations, and referenced anchors. Keep the README dead-link exception narrow.
- For rendered-site changes, inspect the changed page, navigation, code blocks, and tables using development or preview mode. Report manual inspection separately from build success.
- For matrix edits, review the parser/category contracts in [Content Contracts](./content-guidelines.md); a site build does not enforce those semantic rules.
- Keep examples illustrative and credential-free. Do not execute write examples or online lanes to validate a documentation edit.

## Verification Boundaries

[Repository policy](../../../.junie/guidelines.md) keeps tests and documentation/evidence checks local or agent-run. The current [CodeQL workflow](../../../.github/workflows/codeql-analysis.yml) performs .NET build/codegen/packaging checks and analysis; it does not run the docs build or `dotnet test`. A passing Actions run does not verify the site or its live examples.

The frontend package has no lint, standalone typecheck, or browser-test script. Do not report these as passed unless an explicit tool was run and its scope is stated. Likewise, `safe` is online and `sequence-dry-run` only prints the ordered plan; neither is an offline documentation test.
