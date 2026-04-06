namespace AioTieba4DotNet.Transport.Http;

internal sealed class HttpCore : ITiebaHttpCore, IDisposable
{
    private readonly TiebaHttpExecutionPolicy _executionPolicy;
    private readonly bool _ownsHttpClient;
    private bool _disposed;

    internal HttpCore(HttpClient? httpClient = null)
        : this(new TiebaOptions(), httpClient)
    {
    }

    internal HttpCore(TiebaOptions options, HttpClient? httpClient = null,
        bool ownsHttpClient = false)
        : this(TiebaHttpExecutionPolicy.FromOptions(options), httpClient,
            ownsHttpClient)
    {
    }

    internal HttpCore(TiebaHttpExecutionPolicy executionPolicy,
        HttpClient? httpClient = null, bool ownsHttpClient = false)
    {
        _executionPolicy = executionPolicy;
        _ownsHttpClient = httpClient == null || ownsHttpClient;
        HttpClient = httpClient ?? TiebaHttpClientFactory.CreateClient();
        TiebaHttpClientFactory.EnsureEncodingProviderRegistered();
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (_ownsHttpClient) HttpClient.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    public Account? Account { get; private set; }

    public HttpClient HttpClient { get; }

    public void SetAccount(Account newAccount)
    {
        Account = newAccount;
    }

    public async Task<string> SendAsync(Func<HttpRequestMessage> requestFactory, bool allowRetry = false,
        CancellationToken cancellationToken = default)
    {
        using var response = await _executionPolicy.SendAsync(HttpClient,
            _ =>
            {
                var request = requestFactory();
                TiebaHttpRequestMetadata.Apply(request, TiebaHttpRequestKind.Custom, Account);
                return Task.FromResult(request);
            },
            allowRetry, TiebaHttpRequestKind.Custom, cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<string> SendAppFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
        CancellationToken cancellationToken = default)
    {
        using var response = await _executionPolicy.SendAsync(HttpClient,
            TiebaHttpRequestDescriptor.AppForm(uri, data), Account, cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data, CancellationToken cancellationToken = default)
    {
        using var response = await _executionPolicy.SendAsync(HttpClient,
            TiebaHttpRequestDescriptor.AppProto(uri, data), Account, cancellationToken);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public async Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters,
        CancellationToken cancellationToken = default)
    {
        using var response = await _executionPolicy.SendAsync(HttpClient,
            TiebaHttpRequestDescriptor.WebGet(uri, parameters,
                _executionPolicy.HasReadRetries),
            Account,
            cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
        CancellationToken cancellationToken = default)
    {
        using var response = await _executionPolicy.SendAsync(HttpClient,
            TiebaHttpRequestDescriptor.WebForm(uri, data), Account, cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public static List<KeyValuePair<string, string>> Sign(List<KeyValuePair<string, string>> items)
    {
        return TiebaHttpRequestSigner.Sign(items);
    }
}
