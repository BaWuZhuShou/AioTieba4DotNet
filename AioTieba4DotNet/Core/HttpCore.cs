using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AioTieba4DotNet.Abstractions;

namespace AioTieba4DotNet.Core;

public class HttpCore : ITiebaHttpCore
{
    public Account? Account { get; private set; }

    public HttpClient HttpClient { get; }

    public static List<KeyValuePair<string, string>> Sign(List<KeyValuePair<string, string>> items)
    {
        var list = items.Select(item => new KeyValuePair<string, string>(item.Key, item.Value)).ToList();
        list.Add(new KeyValuePair<string, string>("sign", Signer.Sign(items)));
        return list;
    }

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
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private static void SetAppHeaders(HttpRequestMessage request)
    {
        request.Headers.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");
        request.Headers.Add("User-Agent", $"aiotieba/{Const.Version}");
        request.Headers.Add("Host", Const.AppBaseHost);
    }

    private static void SetAppProtoHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("User-Agent", $"aiotieba/{Const.Version}");
        request.Headers.Add("x_bd_data_type", "protobuf");
        request.Headers.Accept.ParseAdd("*/*");
        request.Headers.Connection.Add("keep-alive");
        request.Headers.Add("Host", Const.AppBaseHost);
    }

    private static void SetWebHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("User-Agent", $"aiotieba/{Const.Version}");
        request.Headers.AcceptEncoding.ParseAdd("gzip");
        request.Headers.AcceptEncoding.ParseAdd("deflate");
        request.Headers.CacheControl = new CacheControlHeaderValue
        {
            NoCache = true
        };
        request.Headers.Connection.Add("keep-alive");
        request.Headers.Accept.ParseAdd("*/*");

    }

    public void SetAccount(Account newAccount)
    {
        Account = newAccount;
    }

    // 发送APP请求
    public async Task<HttpResponseMessage> PackAppFormRequestAsync(Uri uri, List<KeyValuePair<string, string>> data)
    {
        var content = new FormUrlEncodedContent(Sign(data));
        var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = content
        };
        SetAppHeaders(request);
        return await HttpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> PackProtoRequestAsync(Uri uri, byte[] data)
    {
        var byteArrayContent = new ByteArrayContent(data);
        byteArrayContent.Headers.Add("Content-Disposition", "form-data; name=\"data\"; filename=\"file\"");
        var content = new MultipartFormDataContent();
        content.Add(byteArrayContent);
        var boundary = content.Headers.ContentType?.Parameters.First(header => header.Name == "boundary");
        if (boundary != null) boundary.Value = boundary.Value?.Replace("\"", "");
        var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = content
        };
        SetAppProtoHeaders(request);
        return await HttpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> PackWebGetRequestAsync(Uri uri, List<KeyValuePair<string, string>> parameters)
    {
        var formUrlEncodedContent = new FormUrlEncodedContent(parameters);
        var readAsStringAsync = await formUrlEncodedContent.ReadAsStringAsync();
        var builder = new UriBuilder(uri) { Query = readAsStringAsync };
        var request = new HttpRequestMessage(HttpMethod.Get, builder.Uri);
        SetWebHeaders(request);
        return await HttpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> PackWebFormRequestAsync(Uri uri, List<KeyValuePair<string, string>> data)
    {
        var content = new FormUrlEncodedContent(data);
        var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = content };
        return await HttpClient.SendAsync(request);
    }
}