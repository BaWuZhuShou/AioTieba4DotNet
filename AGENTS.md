# PROJECT KNOWLEDGE BASE

**Generated:** 2026-03-27
**Commit:** dc756a5
**Branch:** master

## OVERVIEW
AioTieba4DotNet is a multi-target .NET Tieba client library plus tests and a protobuf generator. The maintained product is the C# solution; `aiotieba/` is an upstream Python reference tree kept only for comparison and should not be treated as part of the shipping .NET codebase.

## STRUCTURE
```text
.
├── AioTieba4DotNet/         # main library; architecture rules live in child AGENTS
├── AioTieba4DotNet.Tests/   # MSTest project mirroring source areas
├── ProtoGenerator/          # standalone protobuf code generator
├── docs/                    # user-facing docs and parity backlog
├── .github/                 # CodeQL, publish, release automation
├── .junie/                  # durable AI/project guidance
└── aiotieba/                # upstream Python reference only
```

## WHERE TO LOOK
| Task | Location | Notes |
|------|----------|-------|
| Repo-wide architecture rules | `.junie/guidelines.md` | Durable rule source; update when new core knowledge stabilizes |
| Main C# implementation work | `AioTieba4DotNet/` | Child `AGENTS.md` has library-specific guidance |
| Find public entrypoints | `AioTieba4DotNet/TiebaClient.cs`, `AioTieba4DotNet/DependencyInjection.cs` | Direct client + DI composition |
| Run or extend tests | `AioTieba4DotNet.Tests/` | `TestBase.cs` wires config, account, HttpCore, WebsocketCore |
| Regenerate protobuf outputs | `ProtoGenerator/Program.cs` | Scans `AioTieba4DotNet/Api/**/*.proto` |
| Check missing upstream parity | `docs/todo.md` | Backlog of Python features not yet ported |
| Release / package flow | `.github/workflows/publish.yml` | Tag-driven NuGet + GitHub release |
| Compare upstream behavior | `aiotieba/` | Reference only; do not fold Python-specific rules into C# code |

## CONVENTIONS
- Treat `.junie/guidelines.md` as the authoritative long-lived rule file; AGENTS should stay concise and local.
- Keep repo-level guidance directional here; detailed implementation rules belong in child AGENTS files.
- Package lock files are enabled repo-wide in `Directory.Build.props`.
- CI sets `ContinuousIntegrationBuild`; release publishing is tag-driven.
- There is no repo-specific task runner; use `dotnet` CLI directly.
- `aiotieba/` is for behavior comparison only. User clarification: it is unrelated to the maintained .NET product itself.

## ANTI-PATTERNS (THIS PROJECT)
- Treating `aiotieba/` as part of the maintained C# codebase.
- Hand-editing generated protobuf C# instead of editing `.proto` and regenerating.
- Adding request parameters or business logic that do not exist in upstream `aiotieba` unless explicitly required.
- Manually disposing `HttpResponseMessage` inside API-layer code.
- Assuming every API supports WebSocket; some paths still fall back to HTTP.

## UNIQUE STYLES
- Top-level docs stay selective; most operational detail should live in the nearest child AGENTS file.
- `docs/todo.md` is the working parity map against upstream Python capabilities.
- The compatibility wrapper `AioTieba4DotNet/Client.cs` is obsolete; prefer `TiebaClient`.

## COMMANDS
```bash
dotnet restore --nologo
dotnet build AioTieba4DotNet.sln --configuration Release --no-restore
dotnet test AioTieba4DotNet.Tests/AioTieba4DotNet.Tests.csproj
dotnet run --project ProtoGenerator/ProtoGenerator.csproj
dotnet pack --configuration Release --no-build --output ./nupkg -p:Version=<version> --nologo
```

## NOTES
- If a change alters durable architecture or maintenance rules, mirror that knowledge back into `.junie/guidelines.md`.
- Keep generated / output folders (`bin/`, `obj/`, `TestResults/`) out of any documentation hierarchy decisions.
- Start in `AioTieba4DotNet/AGENTS.md` for implementation work; come back here for repo routing only.
