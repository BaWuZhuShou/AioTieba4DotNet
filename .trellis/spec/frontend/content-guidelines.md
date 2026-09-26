# Documentation Content Contracts

## Preserve the Public v3 Surface

[Repository policy](../../../.junie/guidelines.md) fixes the maintained support surface at `net10.0` and six public modules: `Forums`, `Threads`, `Users`, `Admins`, `Messages`, and `Client`. Verify root accessors against [ITiebaClient.cs](../../../AioTieba4DotNet/Clients/ITiebaClient.cs), methods against [Contracts](../../../AioTieba4DotNet/Contracts), and examples against their implementations.

- Consumer examples use `TiebaClient`, `ITiebaClient`, `AddAioTiebaClient(...)`, or public factories, as shown in [getting-started.md](../../../docs/guide/getting-started.md).
- `Messages` owns message reads, sends, read receipts, and push parsing. `Client` owns lifecycle helpers such as `InitWebSocketAsync`, `InitZIdAsync`, and `SyncAsync`. [messages.md](../../../docs/how-to/messages.md), [IMessagesModule.cs](../../../AioTieba4DotNet/Contracts/IMessagesModule.cs), and [IClientModule.cs](../../../AioTieba4DotNet/Contracts/IClientModule.cs) show this boundary.
- Prefer current public declarations over stale compatibility guidance. Existing policy mentions a `Client.cs` compatibility wrapper, but no such file exists in the current library; [Clients/TiebaClient.cs](../../../AioTieba4DotNet/Clients/TiebaClient.cs) is the concrete entry point. Do not invent an old entry point in a new example.
- Changes to supported frameworks, public exceptions, defaults, or documented behavior require a release/migration review under repository versioning policy.

## Keep the Documentation Owners Distinct

| Question | Authoritative source |
| --- | --- |
| What does the current C# API declare? | Public declarations in `AioTieba4DotNet/Clients/`, `AioTieba4DotNet/Contracts/`, and `AioTieba4DotNet/DependencyInjection.cs` |
| How should a consumer use it? | [reference/modules.md](../../../docs/reference/modules.md), getting-started, and scenario guides |
| Which upstream family and authentication requirements does an implementation follow? | [related/parity.md](../../../docs/related/parity.md) |
| Which public methods have direct online coverage and filterable `Api:*` categories? | [related/public-api-coverage-matrix.md](../../../docs/related/public-api-coverage-matrix.md) |
| Which documentation and evidence files are required locally? | [verify-local.sh](../../../scripts/verify-local.sh) and [verify-local.ps1](../../../scripts/verify-local.ps1) |

The API reference is a consumer cross-check, not a replacement for either ledger. Deferred, indirect, or offline-only coverage rows must not be advertised as directly runnable online API categories. Current test topology is Platform for shared support, Online for runnable scenario discovery, and Governance for the ordered host and contracts; see [Governance/README.md](../../../AioTieba4DotNet.Tests.Governance/README.md).

## Write Examples That Match the Types

[Getting-started examples](../../../docs/guide/getting-started.md) progress from visitor reads through authenticated calls, options, DI, and factory usage. Follow their concrete patterns:

- Include relevant `using` directives and dispose directly created clients with `using var`.
- Use illustrative credential text such as `"你的 BDUSS"` and `"你的 STOKEN"`; never real credentials or production mutation targets.
- Preserve numeric types: message IDs use `long` literals such as `123456789L`, while a forum ID can require `ulong`, shown as `123456789UL`. Check each overload instead of applying one ID type everywhere.
- Preserve nullable/default parameters, argument order, and cancellation support when quoting a full signature. The API reference states the common trailing `CancellationToken cancellationToken = default` convention.
- Label incomplete context honestly. DI examples presume a host `builder`; push parsing presumes bytes supplied by the caller. A fenced block is not proof that an example was compiled or executed.

For example, [IMessagesModule.cs](../../../AioTieba4DotNet/Contracts/IMessagesModule.cs) explains the distinct literals in [the chatroom recipe](../../../docs/how-to/messages.md):

```csharp
await client.Messages.SendChatroomMessageAsync(
    chatroomId: 123456789L,
    forumId: 123456789UL,
    text: "消息内容");
```

## Machine-Read Markdown Needs Contract Review

[PublicApiCoverageMatrixContract.cs](../../../AioTieba4DotNet.Tests.Governance/Contracts/PublicApiCoverageMatrixContract.cs) parses the matrix's exact table header and delimiter and requires nine non-empty columns per row. Preserve the format, allowed dispositions, and category syntax; decorative table edits or a literal pipe inside a cell can break parsing.

[OnlineArchitectureContractTests.cs](../../../AioTieba4DotNet.Tests.Governance/Contracts/OnlineArchitectureContractTests.cs) checks matrix eligibility against declared and discoverable API categories. Editing a verification-command cell can change a tested contract even when the rendered prose looks reasonable.

## Avoid

- Claiming .NET 8/9 support or a maintained v2 release in current guides because an old document mentions it.
- Promising automatic transport fallback for authentication, configuration, cancellation, or business errors; cross-check [advanced.md](../../../docs/guide/advanced.md) and implementation before changing that explanation.
- Describing push parsing as a built-in background event bus; the caller owns payload acquisition and consumption.
- Treating `safe` as offline or claiming live coverage from a site build.
