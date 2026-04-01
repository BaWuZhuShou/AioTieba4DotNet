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
    Http
}
