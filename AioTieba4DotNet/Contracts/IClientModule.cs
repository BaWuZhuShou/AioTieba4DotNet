namespace AioTieba4DotNet.Contracts;

/// <summary>
///     客户端元数据模块契约
/// </summary>
public interface IClientModule
{
    /// <summary>
    ///     初始化 WebSocket 连接
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    Task InitWebSocketAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     初始化客户端 ZId
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>初始化后的 ZId</returns>
    Task<string> InitZIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     同步客户端标识
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>同步后的 ClientId 与 SampleId</returns>
    Task<(string ClientId, string SampleId)> SyncAsync(CancellationToken cancellationToken = default);
}
