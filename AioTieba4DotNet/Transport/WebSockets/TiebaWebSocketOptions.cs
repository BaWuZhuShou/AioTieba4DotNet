namespace AioTieba4DotNet.Transport.WebSockets;

internal sealed record TiebaWebSocketOptions(Uri Endpoint, TimeSpan HeartbeatInterval)
{
    internal static TiebaWebSocketOptions Default { get; } =
        new(new Uri("ws://im.tieba.baidu.com:8000"), TimeSpan.FromSeconds(30));

    internal const string SubProtocol = "chat";
    internal const string ExtensionsHeaderName = "Sec-WebSocket-Extensions";
    internal const string ExtensionsHeaderValue = "im_version=2.3";
    internal const string AcceptEncodingHeaderName = "Accept-Encoding";
    internal const string AcceptEncodingHeaderValue = "gzip";
}
