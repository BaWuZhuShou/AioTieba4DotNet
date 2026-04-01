using AioTieba4DotNet.Session;

namespace AioTieba4DotNet.Transport;

internal sealed record TiebaOperationDescriptor<TResult>(
    string Name,
    TiebaOperationCapabilities Capabilities,
    Func<TiebaClientSession, CancellationToken, Task<TResult>>? ExecuteHttpAsync = null,
    Func<TiebaClientSession, CancellationToken, Task<TResult>>? ExecuteWebSocketAsync = null,
    Action<TiebaClientSession, TResult>? ApplySessionMutation = null);
