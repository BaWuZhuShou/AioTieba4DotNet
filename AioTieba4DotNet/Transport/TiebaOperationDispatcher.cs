using System.Net.WebSockets;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport.WebSockets;

namespace AioTieba4DotNet.Transport;

internal sealed class TiebaOperationDispatcher(TiebaClientSession session)
{
    internal Account RequireAuthenticatedAccount(string operationName)
    {
        return session.RequireAuthenticatedAccount(operationName);
    }

    internal async Task EnsureCanExecuteAsync(string operationName, TiebaOperationCapabilities capabilities,
        CancellationToken cancellationToken = default)
    {
        var descriptor = new TiebaOperationDescriptor<bool>(operationName, capabilities);
        await EnsureSessionRequirementsAsync(descriptor, cancellationToken);
    }

    internal async Task<TResult> ExecuteAsync<TResult>(TiebaOperationDescriptor<TResult> descriptor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        cancellationToken.ThrowIfCancellationRequested();
        await EnsureSessionRequirementsAsync(descriptor, cancellationToken);

        return descriptor.Capabilities.TransportKind switch
        {
            TiebaOperationTransportKind.HttpOnly =>
                await ExecuteHttpAsync(descriptor, cancellationToken),
            TiebaOperationTransportKind.WebSocketPreferred =>
                await ExecuteWebSocketPreferredAsync(descriptor, cancellationToken),
            TiebaOperationTransportKind.WebSocketOnly =>
                await ExecuteWebSocketOnlyAsync(descriptor, cancellationToken),
            _ => throw CreateUnsupportedPathException(descriptor, "The transport kind is not recognized.")
        };
    }

    private async Task EnsureSessionRequirementsAsync<TResult>(TiebaOperationDescriptor<TResult> descriptor,
        CancellationToken cancellationToken)
    {
        if (descriptor.Capabilities.RequiresAuthentication)
            _ = session.RequireAuthenticatedAccount(descriptor.Name);

        if (descriptor.Capabilities.RequiresTbs)
            _ = await session.EnsureTbsAsync(descriptor.Name, cancellationToken);
    }

    private async Task<TResult> ExecuteHttpAsync<TResult>(TiebaOperationDescriptor<TResult> descriptor,
        CancellationToken cancellationToken)
    {
        var executor = descriptor.ExecuteHttpAsync
                       ?? throw CreateUnsupportedPathException(descriptor, "No HTTP execution path exists.");

        return await ExecuteAndApplyMutationAsync(descriptor, executor, cancellationToken);
    }

    private async Task<TResult> ExecuteWebSocketPreferredAsync<TResult>(TiebaOperationDescriptor<TResult> descriptor,
        CancellationToken cancellationToken)
    {
        if (session.Options.TransportMode == TiebaTransportMode.Http ||
            descriptor.ExecuteWebSocketAsync is null)
            return await ExecuteHttpAsync(descriptor, cancellationToken);

        try
        {
            await session.WarmUpWebSocketAsync(descriptor.Name, cancellationToken);
            return await ExecuteAndApplyMutationAsync(descriptor, descriptor.ExecuteWebSocketAsync, cancellationToken);
        }
        catch (Exception exception) when (ShouldFallbackToHttp(exception, cancellationToken))
        {
            if (descriptor.ExecuteHttpAsync is null)
                throw CreateUnsupportedPathException(descriptor,
                    "The WebSocket path is unavailable and no HTTP fallback path exists.", exception);

            return await ExecuteAndApplyMutationAsync(descriptor, descriptor.ExecuteHttpAsync, cancellationToken);
        }
    }

    private async Task<TResult> ExecuteWebSocketOnlyAsync<TResult>(TiebaOperationDescriptor<TResult> descriptor,
        CancellationToken cancellationToken)
    {
        var executor = descriptor.ExecuteWebSocketAsync
                       ?? throw CreateUnsupportedPathException(descriptor, "No WebSocket execution path exists.");

        await session.WarmUpWebSocketAsync(descriptor.Name, cancellationToken);
        return await ExecuteAndApplyMutationAsync(descriptor, executor, cancellationToken);
    }

    private async Task<TResult> ExecuteAndApplyMutationAsync<TResult>(TiebaOperationDescriptor<TResult> descriptor,
        Func<TiebaClientSession, CancellationToken, Task<TResult>> executor,
        CancellationToken cancellationToken)
    {
        var result = await executor(session, cancellationToken);
        descriptor.ApplySessionMutation?.Invoke(session, result);
        return result;
    }

    private static bool ShouldFallbackToHttp(Exception exception, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested || exception is OperationCanceledException)
            return false;

        return exception is WebSocketException or TiebaWebSocketUnavailableException;
    }

    private static TiebaUnsupportedOperationException CreateUnsupportedPathException<TResult>(
        TiebaOperationDescriptor<TResult> descriptor,
        string reason,
        Exception? innerException = null)
    {
        return new TiebaUnsupportedOperationException(
            $"Operation '{descriptor.Name}' has no valid transport path. {reason}", innerException);
    }
}
