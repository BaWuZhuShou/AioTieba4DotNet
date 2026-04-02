namespace AioTieba4DotNet.Transport.WebSockets;

internal sealed record TiebaWebSocketOptions(Uri Endpoint, TimeSpan HeartbeatInterval)
{
    private static readonly Uri DefaultEndpoint = new UriBuilder(Uri.UriSchemeWs, "im.tieba.baidu.com", 8000).Uri;

    internal static TiebaWebSocketOptions Default { get; } =
        new(DefaultEndpoint, TimeSpan.FromSeconds(30));

    internal const string SubProtocol = "chat";
    internal const string ExtensionsHeaderName = "Sec-WebSocket-Extensions";
    internal const string ExtensionsHeaderValue = "im_version=2.3";
    internal const string AcceptEncodingHeaderName = "Accept-Encoding";
    internal const string AcceptEncodingHeaderValue = "gzip";
}
