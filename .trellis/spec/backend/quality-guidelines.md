# Quality and Verification

## Source Conventions

[.editorconfig](../../../.editorconfig) owns style: four spaces, CRLF, UTF-8, final newline, trimmed trailing whitespace, file-scoped C# namespaces, `_camelCase` private fields, PascalCase constants/static readonly fields, and system usings first. It favors `var`, primary constructors, and collection expressions. Follow nearby handwritten code without reformatting generated/unrelated files.

[Directory.Build.props](../../../Directory.Build.props) selects C# 14/.NET 10 and deterministic builds. Analyzer execution and code-style enforcement during builds are currently disabled; compilation does not prove editor suggestions passed. Nullable/implicit-using settings are project-specific; library/generator enable them. [Directory.Packages.props](../../../Directory.Packages.props) owns dependency versions; shared props enable lock files.

Keep public XML docs and defaults aligned. Preserve protocol-required algorithms with narrow suppressions, as in [Session.Account](../../../AioTieba4DotNet/Session/Account.cs). Avoid public DTO data fields, broad suppressions, source-only parity/auth markers, and hand-edited protobuf outputs.

## Build and Documentation Checks

Run from the repository root using the SDK allowed by [global.json](../../../global.json). These commands describe checks; they do not claim execution. Restore needs NuGet access or cached packages.

| Purpose | Command | Effect / prerequisite |
| --- | --- | --- |
| Locked restore | `dotnet restore AioTieba4DotNet.sln --locked-mode` | Fails on manifest/lock disagreement |
| Compile | `dotnet build AioTieba4DotNet.sln --configuration Release --no-restore` | Requires restore; runs no tests |
| Read-only docs/evidence validation | `bash scripts/verify-local.sh --validate-only` | Requires `python` resolving to Python 3 |
| PowerShell equivalent | `pwsh -File scripts/verify-local.ps1 -ValidateOnly` | Same validation intent |
| Ordered plan inspection | `bash scripts/test-lane.sh sequence-dry-run` | Prints plan without executing tests |

[verify-local.sh](../../../scripts/verify-local.sh) and [verify-local.ps1](../../../scripts/verify-local.ps1) normally rewrite the manifest; use validation-only mode for read-only checks. They print docs commands but do not install/build the site or execute tests. See [frontend guidance](../frontend/index.md) for VitePress verification.

The verifiers' Bash `required_docs` and PowerShell `$requiredDocs` lists own mandatory documentation paths. Validation also requires four retained artifacts under `.sisyphus/evidence/`: `parity-truth-freeze.json`, `parity-gap-ledger.json`, `local-verification.manifest.json`, and `local-verification.manifest.schema.json`. This ignored directory may be absent from a checkout; all four files were absent during this bootstrap. Report missing prerequisites without fabricating evidence or weakening validation. Manifest synchronization does not recreate the other artifacts, and historical task evidence cannot replace them.

## Test Topology and Selection

[OnlineTestProjectTopology](../../../AioTieba4DotNet.Tests.Governance/Contracts/OnlineTestProjectTopology.cs) and shared build files define:

- Platform: shared support/environment/fixture gates/execution helpers, not the runnable scenario assembly.
- Online: the discoverability-scanned Safe/Restricted scenario assembly.
- Governance: ordered live suites plus retained offline contracts. Neither its project name nor `Contract:*` categories guarantee offline execution. [ThreadWebSocketOnlineContractTests](../../../AioTieba4DotNet.Tests.Governance/Contracts/ThreadWebSocketOnlineContractTests.cs) is both `Contract:Architecture` and live `Tier:Safe`.

For mapping changes, a specifically inspected offline selection is:

```bash
dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --filter "FullyQualifiedName~AioTieba4DotNet.Tests.Governance.Contracts.MappingCoverageContractTests" -p:CollectCoverage=false
```

This class maps in-memory protobuf/JSON fixtures; it builds/restores as needed but proves neither full-suite success nor coverage. Inspect other selections before running them. Avoid unfiltered Governance/all-solution tests for presumed offline work. Reflection-based contracts can require fresh Release library/Online assemblies; a solution build supplies those outputs.

Assert wire shape, public properties, errors, rollback, and transport selection. Follow [mapping](../../../AioTieba4DotNet.Tests.Governance/Contracts/MappingCoverageContractTests.cs), [WS doubles](../../../AioTieba4DotNet.Tests.Governance/Contracts/ThreadWebSocketDirectContractTests.cs), and [composition](../../../AioTieba4DotNet.Tests.Governance/Contracts/ClientLifecycleAndCompositionContractTests.cs) examples. A log line or nonthrowing call alone does not establish behavior.

## Online Lanes Are Live

[test-lane.sh](../../../scripts/test-lane.sh) / [test-lane.ps1](../../../scripts/test-lane.ps1) require explicit lane arguments. Guide wording about a default safe lane describes policy, not no-argument wrapper behavior.

| Lane | Execution |
| --- | --- |
| `safe` | `Suite:SafeOrdered`: ForumFoundation, ForumExtensions, ThreadRead, UserSocial, Messaging, ThreadWrite |
| `restricted` | `Suite:RestrictedOrdered`: ModerationRestricted, AdminRestricted |
| `sequence-dry-run` | Plan only |

Use `bash scripts/test-lane.sh safe` or `pwsh -File scripts/test-lane.ps1 -Lane safe` only for in-scope live execution with satisfied fixture gates; use `restricted` only when explicitly selected. Safe includes real messages/thread writes and compensation; it is not read-only. Missing credentials do not guarantee no network because guest-safe capabilities can execute. Reuse [OnlineExecutionGate](../../../AioTieba4DotNet.Tests.Platform/Execution/OnlineExecutionGate.cs) and existing environment loading, not ad hoc secrets. Gated/inconclusive results do not prove successful live behavior.

`CompensationAudit` is synthetic suite reporting, not a runnable lane/filter. Advertise direct `Api:*` filters only when supported by the [public API coverage matrix](../../../docs/related/public-api-coverage-matrix.md), excluding deferred rows.

## Coverage and CI

[Directory.Build.targets](../../../Directory.Build.targets) declares repository-total 100% line/branch coverage with narrow generated-code exclusions. Policy scopes maintained handwritten library/generator code, not tests/docs/evidence/upstream Python. Do not lower thresholds or broaden exclusions for a pass.

Declared policy is not proof of achieved/enforced coverage: central packages list a `coverlet.msbuild` version, but active projects do not reference it; wrappers disable collection. Verify collector wiring and actual reports before coverage claims.

[GitHub workflows](../../../.github/workflows/) keep validation to restore/build/codegen/packaging, with CodeQL analysis and release publishing alongside those checks. They do not run `dotnet test` or secret-backed lanes. Local live evidence remains separate from CI build success.

## Public Change Review

Review [README](../../../README.md), [module reference](../../../docs/reference/modules.md), task guides, [parity ledger](../../../docs/related/parity.md), and release/migration notes for public behavior changes. Usage/package identity changes also require the [consumer skill](../../../skills/aiotieba4dotnet/SKILL.md) and references to stay aligned. Durable cross-cutting rules go in nearest policy plus [.junie/guidelines.md](../../../.junie/guidelines.md), without duplicating entire guides.

Assess SemVer from signatures, exception contracts, defaults, documented behavior, and supported TFMs. Spec-only work needs prose/reference/navigation/whitespace checks, not live fixtures or protobuf regeneration.
