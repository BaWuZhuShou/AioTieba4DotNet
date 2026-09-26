# Error Handling

## Public Exception Contract

Types under [Exceptions](../../../AioTieba4DotNet/Exceptions/) are public in root namespace `AioTieba4DotNet`, deriving from `TiebaException`:

| Type | Existing responsibility |
| --- | --- |
| `TiebaAuthenticationException` | Missing credentials required by an operation |
| `TiebaConfigurationException` | Invalid options or missing required session state |
| `TieBaServerException` | Nonzero server error; preserves `Code` and message |
| `TiebaTransportException` | Normalized transport failure |
| `TiebaTimeoutException` | Timeout specialization of `TiebaTransportException` |
| `TiebaProtocolException` | Recognized malformed data, such as an invalid WS frame header |
| `TiebaUnsupportedOperationException` | No valid executor/path |

Preserve the public `TieBaServerException` spelling. Do not rename it or turn business failures into `false`/empty DTOs without an explicit contract change.

[TiebaOptionsValidator](../../../AioTieba4DotNet/Internal/Mapping/TiebaOptionsValidator.cs) rejects STOKEN without BDUSS, negative retries, and nonpositive finite timeouts. [DI](../../../AioTieba4DotNet/DependencyInjection.cs) translates options-validation failure during scoped resolution into `TiebaConfigurationException`. Not every invalid input is wrapped: null guards and account credential-length checks also use standard argument exceptions.

## Response and Transport Boundaries

[ApiResponseValidator](../../../AioTieba4DotNet/Api/ApiResponseValidator.cs) throws for nonzero server codes. JSON defaults are `error_code`/`error_msg`; missing code currently means zero. Preserve family-specific field choices. This is not a strict generic envelope validator, and malformed JSON is not universally translated to `TiebaProtocolException`.

[HTTP execution policy](../../../AioTieba4DotNet/Transport/Http/TiebaHttpExecutionPolicy.cs) normalizes selected send failures through [TiebaHttpErrorNormalizer](../../../AioTieba4DotNet/Transport/Http/TiebaHttpErrorNormalizer.cs). Existing `TiebaException` values stay intact; supported failures become transport/timeout exceptions with an inner exception. Parsing occurs after the send boundary, so not all parser/stream failures receive normalization. [HttpCore](../../../AioTieba4DotNet/Transport/Http/HttpCore.cs) also does not impose a universal HTTP success-status check; family parsing matters.

## Cancellation and Fallback

- Forward `CancellationToken` through protocols, APIs, transport, and locks. Caller-requested `OperationCanceledException` remains cancellation, never a timeout, success, or fallback.
- Session initialization can catch to restore state, but must rethrow and release gates in `finally`; see [TBS service](../../../AioTieba4DotNet/Session/TiebaSessionTbsService.cs).
- [Dispatcher](../../../AioTieba4DotNet/Transport/TiebaOperationDispatcher.cs) fallback is limited to recognized WS unavailability. [GetThreads](../../../AioTieba4DotNet/Api/GetThreads/GetThreads.cs) converts empty/malformed WS payloads to that internal signal while server errors remain observable. Do not add a blanket catch for all protocol failures.
- Preserve cancellation at background-task boundaries so observers can classify expected shutdown. Do not add empty cancellation catches; follow [WebSocket engine](../../../AioTieba4DotNet/Transport/WebSockets/TiebaWebSocketEngine.cs) ownership and cross-cutting policy.

[Direct websocket](../../../AioTieba4DotNet.Tests.Governance/Contracts/ThreadWebSocketDirectContractTests.cs), [wire parity](../../../AioTieba4DotNet.Tests.Governance/Contracts/WireParityContractTests.cs), and [state parity](../../../AioTieba4DotNet.Tests.Governance/Contracts/StateParityContractTests.cs) tests assert error identity, path behavior, and rollback. Logging an exception or checking only absence of exceptions does not verify the contract.
