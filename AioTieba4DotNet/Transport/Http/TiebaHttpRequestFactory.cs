using System.Net.Http.Headers;
using System.Text;
using AioTieba4DotNet.Internal;

namespace AioTieba4DotNet.Transport.Http;

internal static class TiebaHttpRequestFactory
{
    internal static async Task<HttpRequestMessage> CreateMessageAsync(TiebaHttpRequestDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        return descriptor.Kind switch
        {
            TiebaHttpRequestKind.AppForm => CreateAppFormRequest(descriptor),
            TiebaHttpRequestKind.AppProto => CreateAppProtoRequest(descriptor),
            TiebaHttpRequestKind.WebGet => await CreateWebGetRequestAsync(descriptor, cancellationToken),
            TiebaHttpRequestKind.WebForm => CreateWebFormRequest(descriptor),
            _ => throw new InvalidOperationException($"Unsupported request kind '{descriptor.Kind}'.")
        };
    }

    private static HttpRequestMessage CreateAppFormRequest(TiebaHttpRequestDescriptor descriptor)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, descriptor.Uri)
        {
            Content = new FormUrlEncodedContent(TiebaHttpRequestSigner.Sign(descriptor.FormData!))
        };

        request.Headers.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");
        request.Headers.Add("User-Agent", $"aiotieba/{Const.Version}");
        request.Headers.Add("Host", Const.AppBaseHost);
        return request;
    }

    private static HttpRequestMessage CreateAppProtoRequest(TiebaHttpRequestDescriptor descriptor)
    {
        const string boundary = "-*_r1999";
        var prologue = Encoding.ASCII.GetBytes($"--{boundary}\r\nContent-Disposition: form-data; name=\"data\"; filename=\"file\"\r\n\r\n");
        var epilogue = Encoding.ASCII.GetBytes($"\r\n--{boundary}--\r\n");
        var body = new byte[prologue.Length + descriptor.ProtoPayload!.Length + epilogue.Length];
        Buffer.BlockCopy(prologue, 0, body, 0, prologue.Length);
        Buffer.BlockCopy(descriptor.ProtoPayload, 0, body, prologue.Length, descriptor.ProtoPayload.Length);
        Buffer.BlockCopy(epilogue, 0, body, prologue.Length + descriptor.ProtoPayload.Length, epilogue.Length);

        var content = new ByteArrayContent(body);
        content.Headers.TryAddWithoutValidation("Content-Type", $"multipart/form-data; boundary={boundary}");

        var request = new HttpRequestMessage(HttpMethod.Post, descriptor.Uri) { Content = content };
        request.Headers.Add("User-Agent", $"aiotieba/{Const.Version}");
        request.Headers.Add("x_bd_data_type", "protobuf");
        request.Headers.Connection.Add("keep-alive");
        request.Headers.AcceptEncoding.ParseAdd("gzip");
        request.Headers.Add("Host", Const.AppBaseHost);
        return request;
    }

    private static async Task<HttpRequestMessage> CreateWebGetRequestAsync(TiebaHttpRequestDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(descriptor.FormData!);
        var query = await content.ReadAsStringAsync(cancellationToken);
        var builder = new UriBuilder(descriptor.Uri) { Query = query };
        var request = new HttpRequestMessage(HttpMethod.Get, builder.Uri);
        request.Headers.Add("User-Agent", $"aiotieba/{Const.Version}");
        request.Headers.AcceptEncoding.ParseAdd("gzip");
        request.Headers.AcceptEncoding.ParseAdd("deflate");
        request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
        request.Headers.Connection.Add("keep-alive");
        request.Headers.Accept.ParseAdd("*/*");
        return request;
    }

    private static HttpRequestMessage CreateWebFormRequest(TiebaHttpRequestDescriptor descriptor)
    {
        return new HttpRequestMessage(HttpMethod.Post, descriptor.Uri)
        {
            Content = new FormUrlEncodedContent(descriptor.FormData!)
        };
    }
}
