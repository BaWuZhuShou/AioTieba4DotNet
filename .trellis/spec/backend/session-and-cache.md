# Session and Cache Ownership

The library has no database, ORM, migrations, or persistence repository. Stateful seams are the per-client session, transports, and forum lookup cache.

## Client and Account Lifetimes

[TiebaClientComposition](../../../AioTieba4DotNet/Clients/TiebaClientComposition.cs) creates a fresh session/protocol graph per client. [TiebaClient.Dispose](../../../AioTieba4DotNet/Clients/TiebaClient.cs) disposes that session; [TiebaClientSession](../../../AioTieba4DotNet/Session/TiebaClientSession.cs) disposes its gates, TBS service, and disposable transports.

[HttpCore](../../../AioTieba4DotNet/Transport/Http/HttpCore.cs) owns clients it creates; injected `HttpClient` instances are not disposed unless ownership was requested. Preserve direct/DI lifetimes. Use separate factory clients for multiple accounts rather than replacing credentials on a shared singleton.

Distinguish public [Contracts.Account](../../../AioTieba4DotNet/Contracts/Account.cs) credential input from internal [Session.Account](../../../AioTieba4DotNet/Session/Account.cs), which owns credentials, lazy device IDs, and crypto state. [GlobalUsings](../../../AioTieba4DotNet/GlobalUsings.cs) aliases `Account` to the internal type. Public signatures must use the public contract.

The session retains mutable `TiebaOptions`; HTTP policy snapshots timeout/retry values at construction while dispatch reads transport mode from options. There is no unified live-reconfiguration contract. Configure before constructing clients; do not advertise later mutation as supported reconfiguration.

## Authentication and Mutations

[TiebaSessionAuthPolicy](../../../AioTieba4DotNet/Session/TiebaSessionAuthPolicy.cs) separates missing credentials from missing initialized state. Guests are supported; authenticated operations fail through session/dispatcher gates rather than malformed remote requests.

[TiebaSessionState](../../../AioTieba4DotNet/Session/TiebaSessionState.cs) distinguishes guest/authenticated sessions and `Unavailable`, `Pending`, `Initializing`, `Ready` resource states. [TiebaSessionStateStore](../../../AioTieba4DotNet/Session/TiebaSessionStateStore.cs) locks snapshots. Preserve transitions and mirrored account values:

- [TiebaSessionTbsService](../../../AioTieba4DotNet/Session/TiebaSessionTbsService.cs) checks cached TBS, serializes initialization, checks again, and stores a nonempty result. Refresh forces a load. Failure restores state and rethrows; `finally` releases the gate.
- WS warmup, z-id initialization, and sync have session-owned gates and restore prior state on failure.
- [ClientProtocol](../../../AioTieba4DotNet/Protocols/ClientProtocol.cs) applies successful z-id/sync values through `ApplySessionMutation`; executors must not publish partial success earlier.

These gates are operation-specific, not a guarantee of atomicity across all fields/calls. [StateParityContractTests](../../../AioTieba4DotNet.Tests.Governance/Contracts/StateParityContractTests.cs) records rollback, mirrored values, and mutation order, including WS fallback before HTTP mutation.

## Forum Lookup Cache

[ForumInfoCache](../../../AioTieba4DotNet/Internal/ForumInfoCache.cs) stores both name-to-ID and ID-to-name in `MemoryCache`. One runtime instance serves `ForumProtocol` and `AdminProtocol`. ID misses return `0`; name misses return an empty string. Reuse it instead of parallel dictionaries.

Entries currently have no TTL/size limit; the cache exposes no disposal interface. Do not claim durable storage, eviction/freshness policy, automatic refresh, or cross-client sharing.

Avoid global credential/TBS caches, endpoint-local lifecycle gates, swallowed cancellation, and state publication before remote success.
