using AioTieba4DotNet.Exceptions;

namespace AioTieba4DotNet.Transport.Http;

internal static class TiebaHttpErrorNormalizer
{
    internal static Exception Normalize(Exception exception, TiebaHttpRequestKind requestKind, Uri? requestUri,
        bool timedOut, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested && exception is OperationCanceledException)
            return exception;

        if (exception is TiebaException) return exception;

        var target = requestUri?.ToString() ?? "<unknown-uri>";
        if (timedOut || exception is OperationCanceledException)
            return new TiebaTimeoutException($"HTTP {requestKind} request to '{target}' timed out.", exception);

        return new TiebaTransportException($"HTTP {requestKind} request to '{target}' failed.", exception);
    }
}
