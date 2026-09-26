# Transport and Requests

## Operation Dispatch

[TiebaOperationDescriptor](../../../AioTieba4DotNet/Transport/TiebaOperationDescriptor.cs) carries a name, [capabilities](../../../AioTieba4DotNet/Transport/TiebaOperationCapabilities.cs), HTTP/WS executors, and optional successful session mutation. Declare authentication/TBS requirements there. Use `EnsureCanExecuteAsync` before preparatory lookups when authentication must fail first; see [ThreadProtocol.GetRecoversAsync](../../../AioTieba4DotNet/Protocols/ThreadProtocol.cs).

[TiebaOperationDispatcher](../../../AioTieba4DotNet/Transport/TiebaOperationDispatcher.cs) owns selection:

| Capability | Current behavior |
| --- | --- |
| `HttpOnly` | Uses HTTP regardless of configured mode |
| `WebSocketPreferred` | HTTP for `Http` mode or absent WS executor; otherwise warms WS, with `WebSocketOnly` disabling fallback |
| `WebSocketOnly` | Requires WS executor/warmup; does not create an HTTP path |

In `Auto`, preferred operations fall back only for `WebSocketException` or internal `TiebaWebSocketUnavailableException`. Cancellation/server errors do not trigger fallback. HTTP replay after a business error could duplicate work. `ApplySessionMutation` runs after success; see [session ownership](./session-and-cache.md).

## Endpoint Pattern

Pack upstream fields, call `ITiebaHttpCore`/`ITiebaWsCore`, check server errors, and map to public models. Prefer protobuf when supported upstream. [GetThreads](../../../AioTieba4DotNet/Api/GetThreads/GetThreads.cs) demonstrates:

```csharp
Common = new CommonReq { ClientType = 2, ClientVersion = Const.MainVersion },
Pn = pn == 1 ? 0 : pn,
RnNeed = rn + 5,
```

These are family-specific wire rules. Preserve parameter order, special values, command IDs, packing, and auth requirements. Check the [parity ledger](../../../docs/related/parity.md) before copying endpoints.

[JsonApiBase](../../../AioTieba4DotNet/Api/JsonApiBase.cs) delegates to [ApiResponseValidator](../../../AioTieba4DotNet/Api/ApiResponseValidator.cs), including configurable error-field names. For protobuf, check family errors before mapping. Do not assume a universal response envelope.

## HTTP Ownership and Compatibility

- [HttpCore](../../../AioTieba4DotNet/Transport/Http/HttpCore.cs) exposes app-form, app-protobuf, web-GET, web-form, and custom sends. Helpers read/dispose responses; [execution policy](../../../AioTieba4DotNet/Transport/Http/TiebaHttpExecutionPolicy.cs) creates/disposes each attempt's request. Reuse those boundaries.
- [Descriptors](../../../AioTieba4DotNet/Transport/Http/TiebaHttpRequestDescriptor.cs) snapshot inputs. [Request factory](../../../AioTieba4DotNet/Transport/Http/TiebaHttpRequestFactory.cs) owns encoding/multipart framing. Use custom-send factories for different requests rather than bypassing the pipeline.
- [TiebaHttpParityHandler](../../../AioTieba4DotNet/Transport/Http/TiebaHttpParityHandler.cs) normalizes headers, explicit web cookies, and image referers. [TiebaHttpClientFactory](../../../AioTieba4DotNet/Transport/Http/TiebaHttpClientFactory.cs) configures cookies, gzip, code-page support, and infinite `HttpClient.Timeout`; operation policy supplies timeouts.
- Preserve ordered signing in [TiebaHttpRequestSigner](../../../AioTieba4DotNet/Transport/Http/TiebaHttpRequestSigner.cs). Keep compatibility hashes and narrowly justified suppressions instead of swapping algorithms or suppressing whole types. [Signature contracts](../../../AioTieba4DotNet.Tests.Governance/Contracts/SignatureParityContractTests.cs) detect input-order drift.

## Retry and Timeout Limits

[TimeoutConfig](../../../AioTieba4DotNet/Contracts/TimeoutConfig.cs) defaults to 30 seconds and zero retries. Policy uses one linked timeout budget across send attempts. Eligible send exceptions retry only if allowed and neither caller cancellation nor policy timeout occurred. There is no status-code retry/backoff.

`SendWebGetAsync` opts into configured retries; app-form, app-protobuf, and web-form descriptors do not. Custom sends default to `allowRetry: false`. Thus `MaxReadRetryAttempts` does not guarantee every read-only public API retries. Never enable retries globally for writes or rebuild them per endpoint.

Examples: [wire parity contracts](../../../AioTieba4DotNet.Tests.Governance/Contracts/WireParityContractTests.cs) and [direct WS contracts](../../../AioTieba4DotNet.Tests.Governance/Contracts/ThreadWebSocketDirectContractTests.cs) use controlled transports. Preserve [error boundaries](./error-handling.md).
