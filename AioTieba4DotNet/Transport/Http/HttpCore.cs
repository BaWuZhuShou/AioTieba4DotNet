using AioTieba4DotNet.Abstractions;

namespace AioTieba4DotNet.Core;

internal sealed class HttpCore : ITiebaHttpCore, IDisposable
{
    private readonly global::AioTieba4DotNet.Transport.Http.TiebaHttpExecutionPolicy _executionPolicy;
    private readonly bool _ownsHttpClient;
    private bool _disposed;

    internal HttpCore(HttpClient? httpClient = null)
        : this(new global::AioTieba4DotNet.TiebaOptions(), httpClient)
    {
    }

    internal HttpCore(global::AioTieba4DotNet.TiebaOptions options, HttpClient? httpClient = null,
        bool ownsHttpClient = false)
        : this(global::AioTieba4DotNet.Transport.Http.TiebaHttpExecutionPolicy.FromOptions(options), httpClient,
            ownsHttpClient)
    {
    }

    internal HttpCore(global::AioTieba4DotNet.Transport.Http.TiebaHttpExecutionPolicy executionPolicy,
        HttpClient? httpClient = null, bool ownsHttpClient = false)
    {
        _executionPolicy = executionPolicy;
        _ownsHttpClient = httpClient == null || ownsHttpClient;
        HttpClient = httpClient ?? global::AioTieba4DotNet.Transport.Http.TiebaHttpClientFactory.CreateClient();
        global::AioTieba4DotNet.Transport.Http.TiebaHttpClientFactory.EnsureEncodingProviderRegistered();
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
        using var response = await _executionPolicy.SendAsync(HttpClient, _ => Task.FromResult(requestFactory()),
            allowRetry, global::AioTieba4DotNet.Transport.Http.TiebaHttpRequestKind.Custom, cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<string> SendAppFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
        CancellationToken cancellationToken = default)
    {
        using var response = await _executionPolicy.SendAsync(HttpClient,
            global::AioTieba4DotNet.Transport.Http.TiebaHttpRequestDescriptor.AppForm(uri, data), cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data, CancellationToken cancellationToken = default)
    {
        using var response = await _executionPolicy.SendAsync(HttpClient,
            global::AioTieba4DotNet.Transport.Http.TiebaHttpRequestDescriptor.AppProto(uri, data), cancellationToken);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public async Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters,
        CancellationToken cancellationToken = default)
    {
        using var response = await _executionPolicy.SendAsync(HttpClient,
            global::AioTieba4DotNet.Transport.Http.TiebaHttpRequestDescriptor.WebGet(uri, parameters,
                _executionPolicy.HasReadRetries),
            cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
        CancellationToken cancellationToken = default)
    {
        using var response = await _executionPolicy.SendAsync(HttpClient,
            global::AioTieba4DotNet.Transport.Http.TiebaHttpRequestDescriptor.WebForm(uri, data), cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public static List<KeyValuePair<string, string>> Sign(List<KeyValuePair<string, string>> items) =>
        global::AioTieba4DotNet.Transport.Http.TiebaHttpRequestSigner.Sign(items);

    public void Dispose()
    {
        if (_disposed) return;

        if (_ownsHttpClient) HttpClient.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
