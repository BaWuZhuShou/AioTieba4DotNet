# Directory Structure and Public Contracts

## Package Boundaries

| Repository path | Responsibility |
| --- | --- |
| `AioTieba4DotNet/Clients/` | Root-namespace client/factory interfaces, implementations, composition |
| `AioTieba4DotNet/Contracts/` | Module interfaces, options, public account input, transport mode |
| `AioTieba4DotNet/Modules/` | Public facades delegating to internal protocols |
| `AioTieba4DotNet/Protocols/` | Internal orchestration, validation, capability declarations |
| `AioTieba4DotNet/Api/` | Internal upstream request families, packing/parsing, protobuf assets |
| `AioTieba4DotNet/Transport/` | Dispatcher and HTTP/WebSocket machinery |
| `AioTieba4DotNet/Session/` | Internal credentials, device identity, authentication, lifecycle state |
| `AioTieba4DotNet/Models/` | Consumer-facing DTOs and enums |
| `AioTieba4DotNet/Internal/` | Shared helpers and `Mapping/` conversions |
| `AioTieba4DotNet/Exceptions/` | Public exceptions declared in the root namespace |
| `ProtoGenerator/` | Handwritten discovery, planning, execution, console reporting |
| `AioTieba4DotNet.Tests.Platform/` | Shared runtime, environment templates, execution bases, support |
| `AioTieba4DotNet.Tests.Online/` | Discoverability-scanned scenarios under `Tiers/` |
| `AioTieba4DotNet.Tests.Governance/` | Ordered suite host, governance contracts, retained offline tests |
| `aiotieba/` | Upstream comparison source; outside .NET delivery and coverage scope |

The SDK baseline is in [global.json](../../../global.json); [Directory.Build.props](../../../Directory.Build.props) selects `net10.0` and C# 14. [Directory.Packages.props](../../../Directory.Packages.props) owns package versions. Do not introduce multi-targeting as incidental cleanup.

## Public Entry Points

[TiebaClient](../../../AioTieba4DotNet/Clients/TiebaClient.cs), [ITiebaClient](../../../AioTieba4DotNet/Clients/ITiebaClient.cs), [AddAioTiebaClient](../../../AioTieba4DotNet/DependencyInjection.cs), and the [factory](../../../AioTieba4DotNet/Clients/TiebaClientFactory.cs) are the supported entry points. Six module properties remain: `Forums`, `Threads`, `Users`, `Admins`, `Messages`, `Client`.

- `Messages` owns reads, sends, read-state updates, and push parsing; `Client` owns lifecycle helpers such as websocket initialization, z-id initialization, and sync.
- Public contracts use DTOs/enums from `Models/` and `Contracts/`. Requests, session/transport types, and generated protobuf types remain internal.
- Preserve names, defaults, overloads, exceptions, and namespaces. Folder placement does not define the namespace: clients/exceptions use `AioTieba4DotNet`; module implementations currently use `AioTieba4DotNet.Modules`.
- Existing library/cross-cutting guides mention a retained `Client.cs` compatibility facade, but this checkout has no such file. Do not advertise or recreate that absent entry point from guide text. Removing promised contracts still requires an explicit compatibility/migration decision.

## Feature Flow

Follow the existing thread-read path (paths below are relative to the library):

```text
Contracts/IThreadModule.cs -> Modules/ThreadModule.cs
  -> Protocols/ThreadProtocol.cs -> Transport/TiebaOperationDispatcher.cs
  -> Api/GetThreads/GetThreads.cs -> Internal/Mapping/Models/Threads/ThreadsMapper.cs
  -> Models/Threads/Threads.cs
```

[ThreadModule](../../../AioTieba4DotNet/Modules/ThreadModule.cs) supplies defaults and forwards to [ThreadProtocol](../../../AioTieba4DotNet/Protocols/ThreadProtocol.cs). Protocols select capabilities and executors; API families own wire details. Avoid HTTP calls, protobuf parsing, or duplicated authentication in facades.

[TiebaClientComposition.CreateRuntime](../../../AioTieba4DotNet/Clients/TiebaClientComposition.cs) builds the session, dispatcher, protocols, cache, and modules for all entry paths. DI registers `ITiebaClient` scoped and `ITiebaClientFactory` singleton; factory clients receive separate runtimes. Keep composition converged here; see [composition contracts](../../../AioTieba4DotNet.Tests.Governance/Contracts/ClientLifecycleAndCompositionContractTests.cs).

## Placement Rules

- Extend the corresponding interface, module, and protocol for public behavior; add internal `Api/<UpstreamFamily>/` code for wire operations.
- Search existing mappers/request helpers before adding a layer. Preserve upstream export semantics; [parity.md](../../../docs/related/parity.md) is the mapping authority.
- Do not add source-only parity/auth attributes, expose generated types, or infer authentication from method names alone.
- Keep legacy deletion separate from unrelated cleanup; a spec bootstrap does not authorize removing supported contracts.
