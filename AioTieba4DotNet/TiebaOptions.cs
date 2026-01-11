using AioTieba4DotNet.Enums;

namespace AioTieba4DotNet;

/// <summary>
///     贴吧客户端配置项
/// </summary>
public class TiebaOptions
{
    /// <summary>
    ///     BDUSS 登录凭证
    /// </summary>
    public string? Bduss { get; set; }

    /// <summary>
    ///     STOKEN 登录凭证（可选，部分 API 校验所需）
    /// </summary>
    public string? Stoken { get; set; }

    /// <summary>
    ///     默认请求模式（HTTP 或 WebSocket）
    /// </summary>
    public TiebaRequestMode RequestMode { get; set; } = TiebaRequestMode.Http;
}
