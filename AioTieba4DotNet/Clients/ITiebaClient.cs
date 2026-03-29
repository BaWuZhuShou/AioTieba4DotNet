namespace AioTieba4DotNet;

/// <summary>
///     贴吧客户端入口
/// </summary>
public interface ITiebaClient : IDisposable
{
    /// <summary>
    ///     贴吧模块
    /// </summary>
    IForumModule Forums { get; }

    /// <summary>
    ///     主题帖模块
    /// </summary>
    IThreadModule Threads { get; }

    /// <summary>
    ///     用户模块
    /// </summary>
    IUserModule Users { get; }

    /// <summary>
    ///     客户端元数据模块
    /// </summary>
    IClientModule Client { get; }
}
