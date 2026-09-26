# Logging Guidelines

## Implemented Surface

[TiebaLogging](../../../AioTieba4DotNet/TiebaLogging.cs) is a public process-wide opt-in helper over `Microsoft.Extensions.Logging`:

- Initial factory: `NullLoggerFactory.Instance`.
- `GetLogger(string)` / `GetLogger<T>()` create category loggers.
- `EnableFileLog(path, minimumLevel)` replaces/disposes the factory; default minimum is `Information`.
- `Reset()` disposes the current factory and restores the null factory.

The runtime has no internal calls to this helper or automatic request logging pipeline. Enabling it does not provide automatic HTTP traces, DI logger integration, or per-client telemetry. Do not toggle this global facility inside endpoints.

Example of caller-authored logging:

```csharp
using AioTieba4DotNet;
using Microsoft.Extensions.Logging;

TiebaLogging.EnableFileLog("logs/client.log");
try
{
    TiebaLogging.GetLogger("Example").LogInformation("Starting {Operation}", "GetThreads");
}
finally
{
    TiebaLogging.Reset();
}
```

## Format and Limits

The provider appends UTF-8 text with UTC ISO timestamp, level, category, formatted message, and optional exception type/message. A process-wide lock serializes file writes; destination directories are created. Scopes are no-ops; structured properties become text, not JSON fields. There is no rotation, retention, async queue, or automatic redaction. File I/O errors can propagate.

No event-ID registry or project-specific level taxonomy exists. Preserve filtering and use ordinary `ILogger` message templates for new calls; do not invent a promised schema or mandatory runtime logging dependency.

## Diagnostic Boundaries

Keep credentials, cookies, TBS, crypto material, and private messages out of logs. This is the caller's responsibility: [Session.Account](../../../AioTieba4DotNet/Session/Account.cs) holds credential/device state, [HTTP parity handling](../../../AioTieba4DotNet/Transport/Http/TiebaHttpParityHandler.cs) builds credential cookies, and the logger writes formatted text without filtering. Avoid raw options/request/response dumps; inspect exception messages too, because [HTTP errors](../../../AioTieba4DotNet/Transport/Http/TiebaHttpErrorNormalizer.cs) can include the URI.

[ProtoGeneratorApp](../../../ProtoGenerator/ProtoGeneratorApp.cs) writes CLI progress/compiler diagnostics to a supplied `TextWriter`, separately from library logging. Preserve actionable failures and exit status. Test evidence belongs in assertions and established reporting; `Console.WriteLine` does not replace a behavioral assertion.
