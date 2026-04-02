# AioTieba4DotNet Library Guide

## OVERVIEW

This directory contains the maintained v3 .NET 10 library. Public callers come in through `TiebaClient`, `ITiebaClient`,
`AddAioTiebaClient(...)`, and `ITiebaClientFactory`, then work through six public modules: `Forums`, `Threads`, `Users`,
`Admins`, `Messages`, and `Client`.

## STRUCTURE

```text
AioTieba4DotNet/
├── Clients/          # direct client entrypoints and internal composition helpers
├── Contracts/        # public module contracts, options, enums
├── Api/              # internal low-level endpoints, protobuf assets, request families
├── Attributes/       # PythonApi / RequireBduss markers and similar metadata
├── Exceptions/       # public exception types stored in-folder, exposed from root namespace
├── Internal/         # internal helpers and protocol-to-model mapping glue
├── Models/           # public DTOs and enums
├── Modules/          # public business facades over internal protocols and APIs
├── Protocols/        # internal orchestration between modules, session, and transport
├── Session/          # internal auth and lifecycle state
├── Transport/        # internal HTTP, WebSocket, and message transport machinery
├── DependencyInjection.cs
└── GlobalUsings.cs
```

## PUBLIC SURFACE BASELINE

- v3 supports `net10.0` only.
- Public entrypoints remain `TiebaClient`, `ITiebaClient`, `AddAioTiebaClient(...)`, `ITiebaClientFactory`, and
  `TiebaClientFactory`.
- Public module families are `Forums`, `Threads`, `Users`, `Admins`, `Messages`, and `Client`.
- `client.Messages` owns message reads, message sends, read-state updates, and push parsing.
- `client.Client` stays limited to lifecycle helpers such as websocket initialization, z-id initialization, and sync.
- `Client.cs` is a retained compatibility facade. Do not add new feature work there unless the task is explicitly about
  compatibility.

## WHERE TO LOOK

| Task                            | Location                                                                                                                  | Notes                                                                                                                |
|---------------------------------|---------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------|
| DI and composition              | `DependencyInjection.cs`, `Clients/TiebaClient*.cs`                                                                       | Direct, DI, and factory entrypoints should converge on the same composition behavior                                 |
| Public module behavior          | `Modules/*.cs`, `Contracts/*.cs`                                                                                          | Public business surface and public contract shape                                                                    |
| Low-level request families      | `Api/<Feature>/`                                                                                                          | Keep family names and semantics aligned with upstream `aiotieba`                                                     |
| Shared request plumbing         | `Api/JsonApiBase.cs`, `Transport/TiebaOperationDispatcher.cs`, `Transport/ITiebaHttpCore.cs`, `Transport/ITiebaWsCore.cs` | Reuse the current transport, parsing, and dispatcher seams instead of reintroducing superseded request-base patterns |
| Public models                   | `Models/**`                                                                                                               | Keep consumer-facing models protocol-agnostic                                                                        |
| Session and transport internals | `Session/**`, `Transport/**`, `Protocols/**`, `Internal/**`                                                               | Internal only; do not leak these into the public contract                                                            |
| Generated protobuf outputs      | `Api/Protobuf/*.cs`, `Api/*/Protobuf/*.cs`                                                                                | Generated code, never hand-edit                                                                                      |

## CONVENTIONS

- Keep the approved consumer-facing surface at the root `AioTieba4DotNet` namespace plus `Contracts/*` and `Models/*`.
  Folder placement does not automatically mean a consumer-facing namespace.
- Keep low-level `Api/*` classes `internal`. Public callers should reach behavior through modules and contracts, not by
  constructing request classes.
- Mirror upstream `aiotieba` request semantics, naming, packing, parsing, and observable behavior as closely as the C#
  contract allows.
- Add `[PythonApi("aiotieba.api....")]` to API implementations so parity tracing remains searchable.
- Apply `[RequireBduss]` on authenticated API classes.
- Reuse the existing base classes and `HttpCore.Send*Async` helpers instead of reimplementing transport selection,
  parsing, or response disposal.
- Prefer protobuf-backed implementations where upstream supports them, and keep `CommonReq` packing aligned with the
  upstream family.
- Keep protobuf transport and generated types out of the public DTO contract. Public models belong under `Models/**`;
  protocol-to-model conversion belongs under internal mapping code.
- If `.proto` files change, rerun `ProtoGenerator`. Do not patch generated `*.cs` outputs by hand.

## ANTI-PATTERNS

- Exposing `Api/*`, transport internals, or generated protobuf types as the public product surface.
- Inventing request parameters or business logic that upstream `aiotieba` does not have unless a task explicitly
  requires a product-level deviation.
- Spreading message read or push behavior back into `client.Client` now that `Messages` is the public home for that
  family.
- Treating compatibility cleanup tasks as license to remove supported compatibility entrypoints before docs, parity, and
  release notes say the removal is part of the active v3 contract.

## TESTING LINKS

- Deterministic coverage for library behavior lives in `../AioTieba4DotNet.Tests.Deterministic/`.
- Real-link staged verification lives in `../AioTieba4DotNet.Tests.Integration/` and `../AioTieba4DotNet.Tests.Live/`.
- Shared gates, fixtures, cleanup orchestration, and sequencing truth live in `../AioTieba4DotNet.Testing/`.

## NOTES

- If library work changes durable cross-cutting policy, also sync `.junie/guidelines.md`.
- If library work changes user-visible behavior, ensure the docs contract stays aligned with `README.md`, the task
  guides under `docs/how-to/`, `docs/index.md`, `docs/guide/getting-started.md`, `docs/reference/modules.md`,
  `docs/guide/advanced.md`, `docs/guide/troubleshooting.md`, release notes, migration notes, and
  `docs/related/parity-v3.md`.
- If library work changes public usage patterns, install identity, or the consumer-facing module surface, also keep the
  exported skill package under `../skills/aiotieba4dotnet/` aligned, including `SKILL.md`, `skill.json`, and
  `package.json`.
