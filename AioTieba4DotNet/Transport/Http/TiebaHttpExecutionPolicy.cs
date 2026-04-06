using AioTieba4DotNet.Session;

namespace AioTieba4DotNet.Transport.Http;

internal sealed class TiebaHttpExecutionPolicy(TimeSpan requestTimeout, int maxReadRetryAttempts)
{
    internal static TiebaHttpExecutionPolicy Default { get; } = new(TimeSpan.FromSeconds(30), 0);

    internal bool HasReadRetries => maxReadRetryAttempts > 0;

    internal static TiebaHttpExecutionPolicy FromOptions(TiebaOptions options)
    {
        return new TiebaHttpExecutionPolicy(options.RequestTimeout, options.MaxReadRetryAttempts);
    }

    internal async Task<HttpResponseMessage> SendAsync(HttpClient httpClient, TiebaHttpRequestDescriptor descriptor,
        Account? account,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync(httpClient, async ct =>
            {
                var request = await TiebaHttpRequestFactory.CreateMessageAsync(descriptor, ct);
                TiebaHttpRequestMetadata.Apply(request, descriptor.Kind, account);
                return request;
            },
            descriptor.AllowRetry,
            descriptor.Kind, cancellationToken);
    }

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
            catch (HttpRequestException exception)
            {
                const bool timedOut = false;

                if (ShouldRetry(exception, attempt, totalAttempts, timedOut, cancellationToken))
                {
                    lastException = exception;
                    lastAttemptTimedOut = timedOut;
                    continue;
                }

                throw TiebaHttpErrorNormalizer.Normalize(exception, requestKind, lastRequestUri, timedOut,
                    cancellationToken);
            }
            catch (IOException exception)
            {
                const bool timedOut = false;

                if (ShouldRetry(exception, attempt, totalAttempts, timedOut, cancellationToken))
                {
                    lastException = exception;
                    lastAttemptTimedOut = timedOut;
                    continue;
                }

                throw TiebaHttpErrorNormalizer.Normalize(exception, requestKind, lastRequestUri, timedOut,
                    cancellationToken);
            }
            catch (InvalidOperationException exception)
            {
                const bool timedOut = false;

                if (ShouldRetry(exception, attempt, totalAttempts, timedOut, cancellationToken))
                {
                    lastException = exception;
                    lastAttemptTimedOut = timedOut;
                    continue;
                }

                throw TiebaHttpErrorNormalizer.Normalize(exception, requestKind, lastRequestUri, timedOut,
                    cancellationToken);
            }
            catch (OperationCanceledException exception)
            {
                var timedOut = !cancellationToken.IsCancellationRequested && timeoutSource is { IsCancellationRequested: true };

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
