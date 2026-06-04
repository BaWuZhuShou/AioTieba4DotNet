# PROJECT KNOWLEDGE BASE

**Generated:** 2026-03-31
**Branch:** master

## OVERVIEW
AioTieba4DotNet is the maintained .NET 10 Tieba client line. The shipping product is the C# solution rooted in `AioTieba4DotNet/`; `aiotieba/` stays in the repo only as an upstream Python reference for parity checks and implementation comparison.

## STRUCTURE
```text
.
├── AioTieba4DotNet/                   # shipping library; library-specific rules live in child AGENTS
├── AioTieba4DotNet.Tests.Platform/       # shared online runtime support, templates, repo-path helpers, and execution bases
├── AioTieba4DotNet.Tests.Online/         # only discoverability-scanned runnable scenario assembly, with tiers under Tiers/
├── AioTieba4DotNet.Tests.Governance/     # ordered suite host, governance contracts, retained offline contracts, and wrappers
├── ProtoGenerator/                    # protobuf generator for Api/**/*.proto
├── docs/                              # VitePress source docs, related governance docs, and archive notes
├── skills/                            # exported AI skill packages for external installation and reuse
├── .github/                           # build, publish, and release automation
├── .junie/                            # durable maintenance rules and architecture knowledge
└── aiotieba/                          # upstream Python reference only
```

## WHERE TO LOOK
| Task | Location | Notes |
|------|----------|-------|
| Durable cross-cutting rules | `.junie/guidelines.md` | Source of truth for stable maintenance, testing, release, and coverage rules |
| Main library work | `AioTieba4DotNet/` | Start in `AioTieba4DotNet/AGENTS.md` for library boundaries and public surface rules |
| Shared online runtime support | `AioTieba4DotNet.Tests.Platform/` | Environment templates, repo-path helpers, execution bases, and support utilities |
| Unified runnable online scenarios | `AioTieba4DotNet.Tests.Online/` | Safe and Restricted scenarios live under `Tiers/`; this is the only discoverability-scanned scenario assembly |
| Ordered suite host and governance contracts | `AioTieba4DotNet.Tests.Governance/` | Active host for `Suite:SafeOrdered` / `Suite:RestrictedOrdered`, governance contracts, retained offline contracts, and wrapper routes |
| Generator maintenance | `ProtoGenerator/` | Regenerates `.proto` outputs under `AioTieba4DotNet/Api/**/Protobuf` |
| Docs contract and IA | `README.md`, `docs/index.md`, `docs/guide/**`, `docs/how-to/**`, `docs/reference/modules.md`, `docs/related/**`, `docs/archive/todo.md` | README bridges into the active VitePress source tree; related and archive docs stay outside the main how-to/reference path |
| Exported AI skill package | `skills/aiotieba4dotnet/` | Portable consumer-facing skill package with `SKILL.md` and `references/` |
| Parity truth | `docs/related/parity.md` | Authoritative parity ledger for upstream scope, internal implementation mapping, and auth notes |
| Historical backlog only | `docs/archive/todo.md` | Stale history and backlog notes, not active product truth |
| Local verification entrypoints | `scripts/test-lane.*`, `scripts/verify-local.*` | Canonical local and agent-run verification commands |

## CONVENTIONS
- Treat this root guide as repo routing only. Put durable cross-cutting rules in `.junie/guidelines.md`, and put local implementation rules in the nearest child `AGENTS.md`.
- The active product baseline is v3 on `net10.0` only. Do not leave active guide text claiming `net8.0`, `net9.0`, multi-target support, or a live v2 release line.
- `docs/related/parity.md` is the parity truth. `docs/archive/todo.md` is historical context only and must not be presented as an authoritative ledger.
- The active consumer docs IA is `README.md -> docs/index.md -> docs/guide/getting-started.md -> docs/how-to/*.md -> docs/reference/modules.md -> docs/guide/{advanced,troubleshooting}.md -> docs/related/*.md`; `docs/archive/todo.md` stays archive-only.
- The user-facing docs contract is anchored by `README.md` and the required docs list enforced locally by `scripts/verify-local.*`.
- Exported skill packages under `skills/` are distribution artifacts, not repo-local `.agents` helpers. Keep their `SKILL.md`, `references/`, and README mentions aligned when install identity or public usage guidance changes.
- GitHub Actions must stay build-only. They validate restore, build, codegen, and packaging, but they do not run `dotnet test` or invoke local verification contracts.
- The active test topology is `AioTieba4DotNet.Tests.Platform` + `AioTieba4DotNet.Tests.Online` + `AioTieba4DotNet.Tests.Governance` only.
- Local and agent-run online verification routes through `AioTieba4DotNet.Tests.Governance`, with default `safe`, explicit `restricted`, and optional `sequence-dry-run` wrapper output only.
- The retained local verification artifact model is exactly `.sisyphus/evidence/parity-truth-freeze.json`, `.sisyphus/evidence/parity-gap-ledger.json`, `.sisyphus/evidence/local-verification.manifest.json`, and `.sisyphus/evidence/local-verification.manifest.schema.json`.
- `aiotieba/` is reference material only. Never treat it as maintained product code, release scope, or coverage scope.

## ANTI-PATTERNS
- Treating `docs/archive/todo.md` as current parity truth.
- Adding stale guide text that claims the repo still ships multi-target or v2-era baselines.
- Hand-editing generated protobuf C# instead of editing `.proto` files and rerunning `ProtoGenerator`.
- Reintroducing retired concepts such as `deterministic`, `integration`, `live`, or `Cleanup` as if they were still active runnable test paths after the ordered online suite replaced the old lane split.
- Treating compensation audit output as a standalone runnable lane instead of a suite-owned reporting responsibility.

## COMMANDS
```bash
dotnet restore --nologo
dotnet build AioTieba4DotNet.sln --configuration Release --no-restore
pnpm --dir docs install
pnpm --dir docs run build
pwsh ./scripts/verify-local.ps1
pwsh ./scripts/test-lane.ps1 safe
pwsh ./scripts/test-lane.ps1 restricted
pwsh ./scripts/test-lane.ps1 sequence-dry-run
dotnet run --project ProtoGenerator/ProtoGenerator.csproj
dotnet pack --configuration Release --no-build --output ./nupkg -p:Version=<version> --nologo
```

## NOTES
- If a task changes stable maintenance rules, coverage scope, release policy, or test-lane governance, sync that knowledge into `.junie/guidelines.md`.
- If a task changes library boundaries or public surface expectations, sync the local guidance in `AioTieba4DotNet/AGENTS.md`.
- Keep child guides concise and local. Avoid copying large rule blocks across repo root, library, testing, and generator guides.
