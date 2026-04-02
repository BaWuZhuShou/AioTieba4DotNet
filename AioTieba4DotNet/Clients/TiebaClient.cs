namespace AioTieba4DotNet;

/// <summary>
///     默认贴吧客户端实现
/// </summary>
public sealed class TiebaClient : ITiebaClient
{
    private readonly IDisposable _lifetime;

    /// <summary>
    ///     使用凭据创建客户端
    /// </summary>
    /// <param name="bduss">用户 BDUSS</param>
    /// <param name="stoken">用户 STOKEN</param>
    public TiebaClient(string? bduss = null, string? stoken = null)
        : this(new TiebaOptions { Bduss = bduss, Stoken = stoken })
    {
    }

    /// <summary>
    ///     使用公开账户对象创建客户端
    /// </summary>
    /// <param name="account">账户凭据对象</param>
    public TiebaClient(Contracts.Account account)
        : this((account ?? throw new ArgumentNullException(nameof(account))).ToTiebaOptions())
    {
    }

    /// <summary>
    ///     使用配置创建客户端
    /// </summary>
    /// <param name="options">客户端配置</param>
    public TiebaClient(TiebaOptions options)
        : this(TiebaClientComposition.Direct.CreateRuntime(options))
    {
    }

    internal TiebaClient(TiebaClientRuntime runtime)
    {
        _lifetime = runtime.Session;
        Forums = runtime.Forums;
        Threads = runtime.Threads;
        Users = runtime.Users;
        Admins = runtime.Admins;
        Messages = runtime.Messages;
        Client = runtime.Client;
    }

    /// <inheritdoc />
    public IForumModule Forums { get; }

    /// <inheritdoc />
    public IThreadModule Threads { get; }

    /// <inheritdoc />
    public IUserModule Users { get; }

    /// <inheritdoc />
    public IAdminModule Admins { get; }

    /// <inheritdoc />
    public IMessagesModule Messages { get; }

    /// <inheritdoc />
    public IClientModule Client { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        _lifetime.Dispose();
    }
}
