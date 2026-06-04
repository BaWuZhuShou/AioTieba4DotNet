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
├── Attributes/       # reserved for runtime-consumed metadata only; do not reintroduce parity/auth marker attributes
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
- Maintain parity scope, internal implementation mapping, and auth notes in `../docs/related/parity.md` instead of split code attributes or a separate mapping file.
- Do not add source-only parity/auth marker attributes; authenticated API discoverability lives in request/session code plus `../docs/related/parity.md`.
- When upstream protocol compatibility depends on legacy or weak hash algorithms, preserve the required algorithm and keep the compatibility story explicit. Prefer narrow method-level suppressions with clear compatibility justifications over broad type-level suppressions or fake algorithm swaps done only to satisfy static analysis.
- Reuse the existing base classes and `HttpCore.Send*Async` helpers instead of reimplementing transport selection,
  parsing, or response disposal.
- Prefer protobuf-backed implementations where upstream supports them, and keep `CommonReq` packing aligned with the
  upstream family.
- Keep protobuf transport and generated types out of the public DTO contract. Public models belong under `Models/**`;
  protocol-to-model conversion belongs under internal mapping code.
- Prefer shared public DTOs for the same consumer-facing concept across parallel endpoints. Only split public DTO types
  when the endpoint semantics materially differ, and expose public DTO members as properties rather than public fields.
- If `.proto` files change, rerun `ProtoGenerator`. Do not patch generated `*.cs` outputs by hand.

## ANTI-PATTERNS

- Exposing `Api/*`, transport internals, or generated protobuf types as the public product surface.
- Inventing request parameters or business logic that upstream `aiotieba` does not have unless a task explicitly
  requires a product-level deviation.
- Broad compatibility suppressions on whole types when only a few methods need them, or swapping protocol-required hash algorithms just to silence static analysis.
- Spreading message read or push behavior back into `client.Client` now that `Messages` is the public home for that
  family.
- Treating compatibility cleanup tasks as license to remove supported compatibility entrypoints before docs, parity, and
  release notes say the removal is part of the active v3 contract.

## TESTING LINKS

- Shared runtime support, environment templates, repo-path helpers, execution bases, and support utilities live in `../AioTieba4DotNet.Tests.Platform/`.
- The only discoverability-scanned runnable scenario assembly lives in `../AioTieba4DotNet.Tests.Online/`, with Safe and Restricted tiers under `Tiers/`.
- Ordered suite execution, topology and environment contracts, retained offline contract tests, and wrapper-owned `safe` / `restricted` / `sequence-dry-run` routes live in `../AioTieba4DotNet.Tests.Governance/`.

## NOTES

- If library work changes durable cross-cutting policy, also sync `.junie/guidelines.md`.
- If library work changes user-visible behavior, ensure the docs contract stays aligned with `README.md`, the task
  guides under `docs/how-to/`, `docs/index.md`, `docs/guide/getting-started.md`, `docs/reference/modules.md`,
  `docs/guide/advanced.md`, `docs/guide/troubleshooting.md`, release notes, migration notes, and
  `docs/related/parity.md`.
- If library work changes public usage patterns, install identity, or the consumer-facing module surface, also keep the
  exported skill package under `../skills/aiotieba4dotnet/` aligned, including `SKILL.md` and `references/`.
