namespace AioTieba4DotNet;

/// <summary>
///     贴吧客户端配置
/// </summary>
public class TiebaOptions
{
    /// <summary>
    ///     用户 BDUSS
    /// </summary>
    public string? Bduss { get; set; }

    /// <summary>
    ///     用户 STOKEN
    /// </summary>
    public string? Stoken { get; set; }

    /// <summary>
    ///     传输模式
    /// </summary>
    public TiebaTransportMode TransportMode { get; set; } = TiebaTransportMode.Auto;

    /// <summary>
    ///     单次请求超时时间
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     只读 HTTP 请求的最大重试次数
    /// </summary>
    public int MaxReadRetryAttempts { get; set; }
}
