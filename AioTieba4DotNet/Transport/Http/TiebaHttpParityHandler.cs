using System.Net.Http.Headers;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;

namespace AioTieba4DotNet.Transport.Http;

internal sealed class TiebaHttpParityHandler : DelegatingHandler
{
    private const string ImageBaseHost = "imgsrc.baidu.com";
    private const string PortraitBaseHost = "himg.baidu.com";

    public TiebaHttpParityHandler()
    {
    }

    internal TiebaHttpParityHandler(HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Normalize(request);
        return base.SendAsync(request, cancellationToken);
    }

    private static void Normalize(HttpRequestMessage request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestUri = request.RequestUri;
        if (requestUri is null)
            return;

        var requestKind = TiebaHttpRequestMetadata.TryGetRequestKind(request) ?? InferRequestKind(request);
        var account = TiebaHttpRequestMetadata.TryGetAccount(request);

        switch (requestKind)
        {
            case TiebaHttpRequestKind.AppForm:
                ApplyAppHeaders(request);
                break;
            case TiebaHttpRequestKind.AppProto:
                ApplyAppHeaders(request);
                request.Headers.Accept.Clear();
                break;
            case TiebaHttpRequestKind.WebGet:
            case TiebaHttpRequestKind.WebForm:
                ApplyWebHeaders(request);
                ApplyWebCookies(request, account);
                NormalizeImageReferer(request);
                break;
            default:
                NormalizeCustomRequest(request, account);
                break;
        }
    }

    private static TiebaHttpRequestKind InferRequestKind(HttpRequestMessage request)
    {
        var requestUri = request.RequestUri;
        if (requestUri is null)
            return TiebaHttpRequestKind.Custom;

        if (string.Equals(requestUri.Host, Const.AppBaseHost, StringComparison.OrdinalIgnoreCase))
        {
            return request.Content is MultipartFormDataContent
                ? TiebaHttpRequestKind.AppProto
                : TiebaHttpRequestKind.AppForm;
        }

        if (IsWebHost(requestUri.Host))
        {
            return HttpMethod.Get.Equals(request.Method)
                ? TiebaHttpRequestKind.WebGet
                : TiebaHttpRequestKind.WebForm;
        }

        return TiebaHttpRequestKind.Custom;
    }

    private static void NormalizeCustomRequest(HttpRequestMessage request, Account? account)
    {
        var requestUri = request.RequestUri;
        if (requestUri is null)
            return;

        if (string.Equals(requestUri.Host, Const.AppBaseHost, StringComparison.OrdinalIgnoreCase))
        {
            ApplyAppHeaders(request);
            return;
        }

        if (!IsWebHost(requestUri.Host))
            return;

        ApplyWebHeaders(request);
        ApplyWebCookies(request, account);
        NormalizeImageReferer(request);
    }

    private static void ApplyAppHeaders(HttpRequestMessage request)
    {
        ApplyUserAgent(request);
        request.Headers.Host = Const.AppBaseHost;

        request.Headers.AcceptEncoding.Clear();
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

        request.Headers.Connection.Clear();
        request.Headers.Connection.Add("keep-alive");
    }

    private static void ApplyWebHeaders(HttpRequestMessage request)
    {
        ApplyUserAgent(request);

        request.Headers.AcceptEncoding.Clear();
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));

        request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };

        request.Headers.Connection.Clear();
        request.Headers.Connection.Add("keep-alive");
    }

    private static void ApplyWebCookies(HttpRequestMessage request, Account? account)
    {
        if (account is null || request.RequestUri is null)
            return;

        if (!string.Equals(request.RequestUri.Host, Const.WebBaseHost, StringComparison.OrdinalIgnoreCase))
            return;

        if (string.IsNullOrWhiteSpace(account.Bduss) && string.IsNullOrWhiteSpace(account.Stoken))
            return;

        if (request.Headers.Contains("Cookie"))
            return;

        var cookieParts = new List<string>(capacity: 2);
        if (!string.IsNullOrWhiteSpace(account.Bduss))
            cookieParts.Add($"BDUSS={account.Bduss}");

        if (!string.IsNullOrWhiteSpace(account.Stoken))
            cookieParts.Add($"STOKEN={account.Stoken}");

        if (cookieParts.Count > 0)
            request.Headers.TryAddWithoutValidation("Cookie", string.Join("; ", cookieParts));
    }

    private static void NormalizeImageReferer(HttpRequestMessage request)
    {
        var requestUri = request.RequestUri;
        if (requestUri is null || !IsImageHost(requestUri.Host))
            return;

        var refererHeader = request.Headers.Referrer?.ToString();
        if (string.IsNullOrWhiteSpace(refererHeader) && request.Headers.TryGetValues("Referer", out IEnumerable<string>? values))
            refererHeader = values.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(refererHeader))
            return;

        if (Uri.TryCreate(refererHeader, UriKind.Absolute, out Uri? refererUri)
            && string.Equals(refererUri.Host, Const.WebBaseHost, StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.Referrer = null;
            request.Headers.Remove("Referer");
            request.Headers.TryAddWithoutValidation("Referer", Const.WebBaseHost);
        }
    }

    private static void ApplyUserAgent(HttpRequestMessage request)
    {
        request.Headers.UserAgent.Clear();
        request.Headers.TryAddWithoutValidation("User-Agent", $"aiotieba/{Const.Version}");
    }

    private static bool IsImageHost(string host)
    {
        return string.Equals(host, ImageBaseHost, StringComparison.OrdinalIgnoreCase)
               || string.Equals(host, PortraitBaseHost, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWebHost(string host)
    {
        return string.Equals(host, Const.WebBaseHost, StringComparison.OrdinalIgnoreCase)
               || IsImageHost(host);
    }
}
