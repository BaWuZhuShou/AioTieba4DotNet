using System.Net.Http.Headers;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;

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
        var byteArrayContent = new ByteArrayContent(descriptor.ProtoPayload!);
        byteArrayContent.Headers.Add("Content-Disposition", "form-data; name=\"data\"; filename=\"file\"");

        var multipartContent = new MultipartFormDataContent();
        multipartContent.Add(byteArrayContent);

        var boundary = multipartContent.Headers.ContentType!.Parameters.First(header => header.Name == "boundary");
        boundary.Value = boundary.Value!.Replace("\"", string.Empty);

        var request = new HttpRequestMessage(HttpMethod.Post, descriptor.Uri) { Content = multipartContent };
        request.Headers.Add("User-Agent", $"aiotieba/{Const.Version}");
        request.Headers.Add("x_bd_data_type", "protobuf");
        request.Headers.Accept.ParseAdd("*/*");
        request.Headers.Connection.Add("keep-alive");
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

    private static HttpRequestMessage CreateWebFormRequest(TiebaHttpRequestDescriptor descriptor) =>
        new(HttpMethod.Post, descriptor.Uri) { Content = new FormUrlEncodedContent(descriptor.FormData!) };
}
