namespace AioTieba4DotNet.Contracts;

/// <summary>
///     贴吧客户端配置
/// </summary>
public class TiebaOptions
{
    private TimeoutConfig _timeout = new();

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
    public TimeSpan RequestTimeout
    {
        get => _timeout.RequestTimeout;
        set => _timeout =
            new TimeoutConfig { RequestTimeout = value, MaxReadRetryAttempts = _timeout.MaxReadRetryAttempts };
    }

    /// <summary>
    ///     只读 HTTP 请求的最大重试次数
    /// </summary>
    public int MaxReadRetryAttempts
    {
        get => _timeout.MaxReadRetryAttempts;
        set => _timeout = new TimeoutConfig { RequestTimeout = _timeout.RequestTimeout, MaxReadRetryAttempts = value };
    }

    public TimeoutConfig Timeout
    {
        get => _timeout;
        set => _timeout = value ?? new TimeoutConfig();
    }
}
