using AioTieba4DotNet.Contracts;
using System.IO;

namespace AioTieba4DotNet.Transport.Http;

internal sealed class TiebaHttpExecutionPolicy(TimeSpan requestTimeout, int maxReadRetryAttempts)
{
    internal static TiebaHttpExecutionPolicy Default { get; } = new(TimeSpan.FromSeconds(30), 0);

    internal bool HasReadRetries => maxReadRetryAttempts > 0;

    internal static TiebaHttpExecutionPolicy FromOptions(TiebaOptions options) =>
        new(options.RequestTimeout, options.MaxReadRetryAttempts);

    internal async Task<HttpResponseMessage> SendAsync(HttpClient httpClient, TiebaHttpRequestDescriptor descriptor,
        CancellationToken cancellationToken = default) =>
        await SendAsync(httpClient, ct => TiebaHttpRequestFactory.CreateMessageAsync(descriptor, ct), descriptor.AllowRetry,
            descriptor.Kind, cancellationToken);

    internal async Task<HttpResponseMessage> SendAsync(HttpClient httpClient,
        Func<CancellationToken, Task<HttpRequestMessage>> requestFactory,
        bool allowRetry,
        TiebaHttpRequestKind requestKind,
        CancellationToken cancellationToken = default)
    {
        Exception? lastException = null;
        Uri? lastRequestUri = null;
        var lastAttemptTimedOut = false;
        var totalAttempts = allowRetry ? maxReadRetryAttempts + 1 : 1;
        using var timeoutSource = CreateTimeoutSource(cancellationToken);
        var effectiveToken = timeoutSource?.Token ?? cancellationToken;

        for (var attempt = 1; attempt <= totalAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var request = await requestFactory(effectiveToken);
                lastRequestUri = request.RequestUri;
                return await httpClient.SendAsync(request, effectiveToken);
            }
            catch (Exception exception)
            {
                var timedOut = !cancellationToken.IsCancellationRequested && exception is OperationCanceledException &&
                               timeoutSource is { IsCancellationRequested: true };

                if (ShouldRetry(exception, attempt, totalAttempts, timedOut, cancellationToken))
                {
                    lastException = exception;
                    lastAttemptTimedOut = timedOut;
                    continue;
                }

                throw TiebaHttpErrorNormalizer.Normalize(exception, requestKind, lastRequestUri, timedOut,
                    cancellationToken);
            }
        }

        throw TiebaHttpErrorNormalizer.Normalize(lastException!, requestKind, lastRequestUri, lastAttemptTimedOut,
            cancellationToken);
    }

    private static bool ShouldRetry(Exception exception, int attempt, int totalAttempts, bool timedOut,
        CancellationToken cancellationToken)
    {
        if (attempt >= totalAttempts || cancellationToken.IsCancellationRequested || timedOut) return false;
        return exception is HttpRequestException or IOException ||
               exception is OperationCanceledException;
    }

    private CancellationTokenSource? CreateTimeoutSource(CancellationToken cancellationToken)
    {
        if (requestTimeout == Timeout.InfiniteTimeSpan) return null;

        var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        source.CancelAfter(requestTimeout);
        return source;
    }
}
