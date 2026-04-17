namespace AioTieba4DotNet.Contracts;

/// <summary>
///     客户端传输模式
/// </summary>
public enum TiebaTransportMode
{
    /// <summary>
    ///     自动选择可用传输
    /// </summary>
    Auto,

    /// <summary>
    ///     仅使用 HTTP
    /// </summary>
    Http,

    /// <summary>
    ///     对支持 WebSocket 的调用仅使用 WebSocket，不允许回退到 HTTP
    /// </summary>
    WebSocketOnly
}
