using AioTieba4DotNet.Core;

namespace AioTieba4DotNet.Abstractions;

/// <summary>
///     WebSocket 核心接口，负责维护与贴吧长连接
/// </summary>
public interface ITiebaWsCore
{
    /// <summary>
    ///     当前绑定的账户信息
    /// </summary>
    Account? Account { get; }

    /// <summary>
    ///     设置或更新绑定的账户
    /// </summary>
    void SetAccount(Account newAccount);

    /// <summary>
    ///     建立 WebSocket 连接
    /// </summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     发送原始 Protobuf 请求（不等待响应）
    /// </summary>
    Task SendAsync(WSReq req, CancellationToken cancellationToken = default);

    /// <summary>
    ///     发送业务请求并等待响应
    /// </summary>
    /// <param name="cmd">指令号</param>
    /// <param name="data">业务负载字节流</param>
    /// <param name="encrypt">是否对负载加密（默认开启）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>服务器返回的响应对象</returns>
    Task<WSRes> SendAsync(int cmd, byte[] data, bool encrypt = true, CancellationToken cancellationToken = default);

    /// <summary>
    ///     监听并流式获取服务器推送消息（如通知等）
    /// </summary>
    IAsyncEnumerable<WSRes> ListenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     关闭连接并清理资源
    /// </summary>
    Task CloseAsync(CancellationToken cancellationToken = default);
}
