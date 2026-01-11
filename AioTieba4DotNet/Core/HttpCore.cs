using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Exceptions;

namespace AioTieba4DotNet.Core;

/// <summary>
/// HTTP 核心实现类，负责底层网络请求的打包、签名与发送
/// </summary>
public class HttpCore : ITiebaHttpCore
{
    /// <summary>
    /// 当前绑定的账户信息
    /// </summary>
    public Account? Account { get; private set; }

    /// <summary>
    /// 用于发送请求的 HttpClient 实例
    /// </summary>
    public HttpClient HttpClient { get; }

    /// <summary>
    /// 对请求参数进行签名
    /// </summary>
    /// <param name="items">待签名的参数列表</param>
    /// <returns>包含签名后的参数列表</returns>
    public static List<KeyValuePair<string, string>> Sign(List<KeyValuePair<string, string>> items)
    {
        var list = items.Select(item => new KeyValuePair<string, string>(item.Key, item.Value)).ToList();
        list.Add(new KeyValuePair<string, string>("sign", Signer.Sign(items)));
        return list;
    }

    /// <summary>
    /// 初始化 HttpCore
    /// </summary>
    /// <param name="httpClient">可选的 HttpClient 实例，若不提供则自动创建一个</param>
    public HttpCore(HttpClient? httpClient = null)
    {
        if (httpClient == null)
        {
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new CookieContainer(),
                AutomaticDecompression = DecompressionMethods.GZip
            };

            HttpClient = new HttpClient(handler);
        }
        else
        {
            HttpClient = httpClient;
        }

        // 注册 GBK 等编码支持，部分旧版 Web API 依赖
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// 设置 App 端请求通用 Header
    /// </summary>
    private static void SetAppHeaders(HttpRequestMessage request)
    {
        request.Headers.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");
        request.Headers.Add("User-Agent", $"aiotieba/{Const.Version}");
        request.Headers.Add("Host", Const.AppBaseHost);
    }

    /// <summary>
    /// 设置 App 端 Protobuf 请求通用 Header
    /// </summary>
    private static void SetAppProtoHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("User-Agent", $"aiotieba/{Const.Version}");
        request.Headers.Add("x_bd_data_type", "protobuf");
        request.Headers.Accept.ParseAdd("*/*");
        request.Headers.Connection.Add("keep-alive");
        request.Headers.Add("Host", Const.AppBaseHost);
    }

    /// <summary>
    /// 设置 Web 端请求通用 Header
    /// </summary>
    private static void SetWebHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("User-Agent", $"aiotieba/{Const.Version}");
        request.Headers.AcceptEncoding.ParseAdd("gzip");
        request.Headers.AcceptEncoding.ParseAdd("deflate");
        request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
        request.Headers.Connection.Add("keep-alive");
        request.Headers.Accept.ParseAdd("*/*");
    }

    /// <summary>
    /// 绑定账户信息
    /// </summary>
    /// <param name="newAccount">账户实例</param>
    public void SetAccount(Account newAccount)
    {
        Account = newAccount;
    }

    /// <summary>
    /// 发送 App 端表单请求（自动添加签名）
    /// </summary>
    private async Task<HttpResponseMessage> PackAppFormRequestAsync(Uri uri, List<KeyValuePair<string, string>> data)
    {
        using var content = new FormUrlEncodedContent(Sign(data));
        using var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = content };
        SetAppHeaders(request);
        return await HttpClient.SendAsync(request);
    }

    /// <summary>
    /// 发送 App 端 Protobuf 请求（自动添加签名）
    /// </summary>
    private async Task<HttpResponseMessage> PackProtoRequestAsync(Uri uri, byte[] data)
    {
        using var byteArrayContent = new ByteArrayContent(data);
        byteArrayContent.Headers.Add("Content-Disposition", "form-data; name=\"data\"; filename=\"file\"");
        using var content = new MultipartFormDataContent();
        content.Add(byteArrayContent);
        var boundary = content.Headers.ContentType?.Parameters.First(header => header.Name == "boundary");
        if (boundary != null) boundary.Value = boundary.Value?.Replace("\"", "");
        using var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = content };
        SetAppProtoHeaders(request);
        return await HttpClient.SendAsync(request);
    }

    /// <summary>
    /// 发送 Web 端 GET 请求
    /// </summary>
    private async Task<HttpResponseMessage> PackWebGetRequestAsync(Uri uri,
        List<KeyValuePair<string, string>> parameters)
    {
        using var formUrlEncodedContent = new FormUrlEncodedContent(parameters);
        var readAsStringAsync = await formUrlEncodedContent.ReadAsStringAsync();
        var builder = new UriBuilder(uri) { Query = readAsStringAsync };
        using var request = new HttpRequestMessage(HttpMethod.Get, builder.Uri);
        SetWebHeaders(request);
        return await HttpClient.SendAsync(request);
    }

    /// <summary>
    /// 发送 Web 端表单请求
    /// </summary>
    private async Task<HttpResponseMessage> PackWebFormRequestAsync(Uri uri, List<KeyValuePair<string, string>> data)
    {
        using var content = new FormUrlEncodedContent(data);
        using var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = content };
        return await HttpClient.SendAsync(request);
    }

    private void CheckBdussRequirement()
    {
        var stackTrace = new StackTrace(false);
        foreach (var frame in stackTrace.GetFrames())
        {
            var method = frame.GetMethod();
            var type = method?.DeclaringType;

            if (type == null || type == typeof(HttpCore)) continue;

            var hasAttr = type.GetCustomAttribute<RequireBdussAttribute>() != null ||
                          method!.GetCustomAttribute<RequireBdussAttribute>() != null;

            if (hasAttr) throw new TiebaException("Account not set or BDUSS missing. This API requires BDUSS.");

            return;
        }
    }

    /// <summary>
    /// 发送 App 端表单请求并获取字符串响应
    /// </summary>
    public async Task<string> SendAppFormAsync(Uri uri, List<KeyValuePair<string, string>> data)
    {
        if (string.IsNullOrEmpty(Account?.Bduss)) CheckBdussRequirement();

        using var response = await PackAppFormRequestAsync(uri, data);
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// 发送 App 端 Protobuf 请求并获取字节数组响应
    /// </summary>
    public async Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data)
    {
        if (string.IsNullOrEmpty(Account?.Bduss)) CheckBdussRequirement();

        using var response = await PackProtoRequestAsync(uri, data);
        return await response.Content.ReadAsByteArrayAsync();
    }

    /// <summary>
    /// 发送 Web 端 GET 请求并获取字符串响应
    /// </summary>
    public async Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters)
    {
        if (string.IsNullOrEmpty(Account?.Bduss)) CheckBdussRequirement();

        using var response = await PackWebGetRequestAsync(uri, parameters);
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// 发送 Web 端表单请求并获取字符串响应
    /// </summary>
    public async Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data)
    {
        if (string.IsNullOrEmpty(Account?.Bduss)) CheckBdussRequirement();

        using var response = await PackWebFormRequestAsync(uri, data);
        return await response.Content.ReadAsStringAsync();
    }
}
