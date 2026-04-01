# PROJECT KNOWLEDGE BASE

**Generated:** 2026-03-31
**Branch:** master

## OVERVIEW
AioTieba4DotNet is the maintained .NET 10 Tieba client line. The shipping product is the C# solution rooted in `AioTieba4DotNet/`; `aiotieba/` stays in the repo only as an upstream Python reference for parity checks and implementation comparison.

## STRUCTURE
```text
.
├── AioTieba4DotNet/                   # shipping library; library-specific rules live in child AGENTS
├── AioTieba4DotNet.Testing/           # shared test infrastructure, fixture gates, sequencing manifest
├── AioTieba4DotNet.Tests.Deterministic/ # offline deterministic and coverage-bearing tests
├── AioTieba4DotNet.Tests.Integration/ # controlled real-link verification
├── AioTieba4DotNet.Tests.Live/        # credentialed and mutation-capable verification
├── ProtoGenerator/                    # protobuf generator for Api/**/*.proto
├── docs/                              # user-facing docs plus parity and migration docs
├── .github/                           # build, publish, and release automation
├── .junie/                            # durable maintenance rules and architecture knowledge
└── aiotieba/                          # upstream Python reference only
```

## WHERE TO LOOK
| Task | Location | Notes |
|------|----------|-------|
| Durable cross-cutting rules | `.junie/guidelines.md` | Source of truth for stable maintenance, testing, release, and coverage rules |
| Main library work | `AioTieba4DotNet/` | Start in `AioTieba4DotNet/AGENTS.md` for library boundaries and public surface rules |
| Shared test sequencing and fixtures | `AioTieba4DotNet.Testing/` | `TestBase`, fixture gates, cleanup orchestration, sequencing manifest |
| Deterministic verification | `AioTieba4DotNet.Tests.Deterministic/` | Offline tests and coverage-bearing lane |
| Integration verification | `AioTieba4DotNet.Tests.Integration/` | Staged real-link checks using the sequencing manifest |
| Live verification | `AioTieba4DotNet.Tests.Live/` | Credentialed, mutation-capable, cleanup-aware scenarios |
| Generator maintenance | `ProtoGenerator/` | Regenerates `.proto` outputs under `AioTieba4DotNet/Api/**/Protobuf` |
| Docs contract and IA | `README.md`, `docs/getting-started.md`, `docs/*.md` | README routes to task guides, reference, advanced, troubleshooting, migration, release notes, and parity ledger |
| Parity truth | `docs/parity-v3.md` | Authoritative v3 parity ledger against upstream export scope |
| Historical backlog only | `docs/todo.md` | Stale history and backlog notes, not active product truth |
| Local verification entrypoints | `scripts/test-lane.*`, `scripts/verify-local.*` | Canonical local and agent-run verification commands |

## CONVENTIONS
- Treat this root guide as repo routing only. Put durable cross-cutting rules in `.junie/guidelines.md`, and put local implementation rules in the nearest child `AGENTS.md`.
- The active product baseline is v3 on `net10.0` only. Do not leave active guide text claiming `net8.0`, `net9.0`, multi-target support, or a live v2 release line.
- `docs/parity-v3.md` is the parity truth. `docs/todo.md` is historical context only and must not be presented as the authoritative parity ledger.
- The user-facing docs contract is anchored by `README.md` and the required docs list enforced by `scripts/verify-local.*`.
- GitHub Actions must stay build-only. They validate restore, build, codegen, docs contract, packaging, and evidence presence, but they do not run `dotnet test`.
- Local and agent-run verification uses four lanes only: `deterministic`, `integration`, `live`, and `sequence-dry-run`.
- `aiotieba/` is reference material only. Never treat it as maintained product code, release scope, or coverage scope.

## ANTI-PATTERNS
- Treating `docs/todo.md` as current parity truth.
- Adding stale guide text that claims the repo still ships multi-target or v2-era baselines.
- Hand-editing generated protobuf C# instead of editing `.proto` files and rerunning `ProtoGenerator`.
- Putting test-lane or cleanup policy in GitHub Actions instead of the local verification scripts and sequencing manifest.
- Treating the `Cleanup` sequencing wave as a runnable MSTest category filter instead of a synthetic compensation stage.

## COMMANDS
```bash
dotnet restore --nologo
dotnet build AioTieba4DotNet.sln --configuration Release --no-restore
pwsh ./scripts/verify-local.ps1
pwsh ./scripts/test-lane.ps1 deterministic
pwsh ./scripts/test-lane.ps1 integration
pwsh ./scripts/test-lane.ps1 live
pwsh ./scripts/test-lane.ps1 sequence-dry-run
dotnet run --project ProtoGenerator/ProtoGenerator.csproj
dotnet pack --configuration Release --no-build --output ./nupkg -p:Version=<version> --nologo
```

## NOTES
- If a task changes stable maintenance rules, coverage scope, release policy, or test-lane governance, sync that knowledge into `.junie/guidelines.md`.
- If a task changes library boundaries or public surface expectations, sync the local guidance in `AioTieba4DotNet/AGENTS.md`.
- Keep child guides concise and local. Avoid copying large rule blocks across repo root, library, testing, and generator guides.
