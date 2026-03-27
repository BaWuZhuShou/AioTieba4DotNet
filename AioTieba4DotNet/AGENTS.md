# AioTieba4DotNet Library Guide

## OVERVIEW
This directory is the maintained C# library. Public surface area lives in `TiebaClient`, DI registration, public entities, and `Modules/*`; low-level API implementations stay internal.

## STRUCTURE
```text
AioTieba4DotNet/
├── Abstractions/   # public contracts for client, modules, and core seams
├── Api/            # low-level endpoint implementations, feature entities, protobuf assets
├── Attributes/     # metadata such as PythonApi / RequireBduss
├── Core/           # transport, auth, crypto, cache, factory, async init helpers
├── Entities/       # shared public entities
├── Enums/          # request modes and shared enums
├── Exceptions/     # TiebaException / TieBaServerException surface
├── Modules/        # public business facades over Api layer
├── DependencyInjection.cs
└── TiebaClient.cs
```

## WHERE TO LOOK
| Task | Location | Notes |
|------|----------|-------|
| Register library in DI | `DependencyInjection.cs` | Adds HttpClient, cores, modules, client factory |
| Direct client composition | `TiebaClient.cs`, `Core/TiebaClientFactory.cs` | Manual and DI-based entrypoints |
| Add or port a low-level endpoint | `Api/<Feature>/` | Keep file/folder names aligned with upstream behavior |
| Reuse request plumbing | `Api/ApiBase.cs`, `Api/JsonApiBase.cs`, `Api/ProtoApiBase.cs` | Error checks, parsing, dual-mode dispatch |
| Expose user-facing behavior | `Modules/*.cs` | Public surface should call internal Api implementations |
| Transport / auth / cache changes | `Core/` | `HttpCore`, `WebsocketCore`, `Signer`, `TbCrypto`, `ForumInfoCache`, `AsyncInit<T>` |
| Shared public contracts | `Abstractions/`, `Entities/` | Keep protocol-specific types out of consumer surface |
| Update tests with behavior changes | `../AioTieba4DotNet.Tests/` | Mirror source area and reuse `TestBase` |

## CONVENTIONS
- `Modules/*`, `TiebaClient`, and `DependencyInjection.AddAioTiebaClient` are the public entry surface.
- `Api/*` classes, including base classes, stay `internal`; do not expose low-level request classes directly.
- New APIs should mirror upstream `aiotieba` behavior and naming as closely as practical.
- Add `[PythonApi("aiotieba.api....")]` to every API implementation so upstream mapping stays searchable.
- Apply `[RequireBduss]` on authenticated APIs.
- Reuse `JsonApiBase`, `ProtoApiBase`, `ApiWsBase<TResult>`, or `ProtoApiWsBase<TResult>` instead of open-coded request pipelines.
- Prefer protobuf when upstream supports it; include `CommonReq` and keep field packing aligned with upstream.
- Public XML docs should use `<see cref="..."/>` for complex public types.
- Entity conversion helpers such as `FromTbData` stay `internal`.
- `*/Protobuf/*.cs` files are generated outputs. Edit `.proto`, then rerun `ProtoGenerator`; never patch generated C# by hand.
- Use `HttpCore.Send*Async` helpers from API code; do not manually manage `HttpResponseMessage` disposal there.
- Use `AsyncInit<T>` for shared async one-time initialization and `ForumInfoCache` for forum name/Fid translation.

## ANTI-PATTERNS
- Putting public feature logic directly under `Api/` instead of exposing it through `Modules/*`.
- Re-implementing `CheckError`, `ParseBody`, or request-mode dispatch instead of reusing the base classes.
- Exposing protobuf transport types as user-facing entities.
- Inventing request parameters or protocol behavior that upstream `aiotieba` does not have unless explicitly requested.
- Adding new functionality to obsolete compatibility wrapper `Client.cs` unless the goal is backward compatibility.

## NOTES
- `Client.cs` is an obsolete compatibility facade; prefer `TiebaClient` in new code and docs.
- If a change introduces durable library rules, sync them back into `../.junie/guidelines.md`.
- Keep `Api/Protobuf/` and feature-local `Protobuf/` folders documented by parent guidance only; they do not need their own AGENTS file.
