using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Enums;
using AioTieba4DotNet.Modules;

namespace AioTieba4DotNet;

/// <summary>
///     贴吧客户端实现类，是所有功能模块的统一入口
/// </summary>
public class TiebaClient : ITiebaClient
{
    /// <summary>
    ///     使用简单的凭证初始化客户端
    /// </summary>
    /// <param name="bduss">BDUSS 凭证</param>
    /// <param name="stoken">STOKEN 凭证（可选）</param>
    public TiebaClient(string? bduss = null, string? stoken = null) : this(new TiebaOptions
    {
        Bduss = bduss, Stoken = stoken
    })
    {
    }

    /// <summary>
    ///     使用配置项初始化客户端
    /// </summary>
    /// <param name="options">配置参数</param>
    public TiebaClient(TiebaOptions options) : this(CreateHttpCore(options))
    {
        RequestMode = options.RequestMode;
    }

    /// <summary>
    ///     使用现有的 HTTP 核心组件初始化客户端
    /// </summary>
    /// <param name="httpCore">HTTP 核心组件实例</param>
    public TiebaClient(ITiebaHttpCore httpCore)
    {
        HttpCore = httpCore;
        WsCore = new WebsocketCore(httpCore.Account);
        Forums = new ForumModule(httpCore);
        Threads = new ThreadModule(httpCore, Forums, WsCore);
        Users = new UserModule(httpCore, Forums, WsCore);
        Client = new ClientModule(httpCore);
    }

    /// <summary>
    ///     全量手动注入初始化（通常由依赖注入框架调用）
    /// </summary>
    public TiebaClient(ITiebaHttpCore httpCore, IForumModule forums, IThreadModule threads, IUserModule users,
        IClientModule client, ITiebaWsCore wsCore)
    {
        HttpCore = httpCore;
        Forums = forums;
        Threads = threads;
        Users = users;
        Client = client;
        WsCore = wsCore;
    }

    /// <summary>
    ///     获取或设置全局请求模式（HTTP 或 WebSocket）
    /// </summary>
    public TiebaRequestMode RequestMode
    {
        get => Threads.RequestMode;
        set
        {
            Threads.RequestMode = value;
            Users.RequestMode = value;
        }
    }

    /// <summary>
    ///     HTTP 核心组件，处理网络请求与拦截器
    /// </summary>
    public ITiebaHttpCore HttpCore { get; }

    /// <summary>
    ///     贴吧/吧务模块
    /// </summary>
    public IForumModule Forums { get; }

    /// <summary>
    ///     帖子/回复模块
    /// </summary>
    public IThreadModule Threads { get; }

    /// <summary>
    ///     用户/社交模块
    /// </summary>
    public IUserModule Users { get; }

    /// <summary>
    ///     客户端底层模块
    /// </summary>
    public IClientModule Client { get; }

    /// <summary>
    ///     WebSocket 核心组件，维护实时连接
    /// </summary>
    public ITiebaWsCore WsCore { get; }

    /// <summary>
    ///     释放 WebSocket 资源及连接
    /// </summary>
    public void Dispose()
    {
        (WsCore as IDisposable)?.Dispose();
    }

    private static ITiebaHttpCore CreateHttpCore(TiebaOptions options)
    {
        var httpCore = new HttpCore();
        if (!string.IsNullOrEmpty(options.Bduss))
            httpCore.SetAccount(new Account(options.Bduss, options.Stoken ?? string.Empty));

        return httpCore;
    }
}
