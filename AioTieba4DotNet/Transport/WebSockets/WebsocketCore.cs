namespace AioTieba4DotNet.Transport.WebSockets;

/// <summary>
///     WebSocket 核心实现类，负责维护与贴吧的实时双向长连接
/// </summary>
internal sealed class WebsocketCore : ITiebaWsCore, IDisposable
{
    private readonly TiebaWebSocketEngine _engine;
    private readonly TiebaWebSocketFrameCodec _frameCodec;
    private readonly object _accountLock = new();

    /// <summary>
    ///     初始化 WebsocketCore
    /// </summary>
    /// <param name="account">可选的账户信息</param>
    internal WebsocketCore(Account? account = null)
        : this(account, new TiebaWebSocketFrameCodec(), new TiebaWebSocketHandshakeBuilder(),
            new ClientWebSocketConnectionFactory(TiebaWebSocketOptions.Default), TiebaWebSocketOptions.Default,
            SystemTiebaWebSocketDelayStrategy.Instance)
    {
    }

    internal WebsocketCore(Account? account, TiebaWebSocketFrameCodec frameCodec,
        TiebaWebSocketHandshakeBuilder handshakeBuilder, ITiebaWebSocketConnectionFactory connectionFactory,
        TiebaWebSocketOptions options, ITiebaWebSocketDelayStrategy delayStrategy)
    {
        Account = account;
        _frameCodec = frameCodec;
        _engine = new TiebaWebSocketEngine(() => Account, frameCodec, handshakeBuilder, connectionFactory, options,
            delayStrategy);
    }

    /// <summary>
    ///     释放 WebSocket 引擎与底层连接资源。
    /// </summary>
    public void Dispose()
    {
        _engine.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     当前绑定的账户信息
    /// </summary>
    public Account? Account { get; private set; }

    /// <summary>
    ///     绑定账户信息
    /// </summary>
    public void SetAccount(Account newAccount)
    {
        Account = newAccount;
    }

    private Account EnsureRuntimeAccount()
    {
        if (Account != null)
            return Account;

        lock (_accountLock)
        {
            Account ??= new Account();
            return Account;
        }
    }

    /// <summary>
    ///     建立 WebSocket 连接并执行初始化握手
    /// </summary>
    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        _ = EnsureRuntimeAccount();
        return _engine.ConnectAsync(cancellationToken);
    }

    /// <summary>
    ///     发送原始 WSReq 对象
    /// </summary>
    public Task SendAsync(WSReq req, CancellationToken cancellationToken = default)
    {
        _ = EnsureRuntimeAccount();
        return _engine.SendAsync(req, cancellationToken);
    }

    /// <summary>
    ///     发送业务请求并异步等待对应的响应
    /// </summary>
    public Task<WSRes> SendAsync(int cmd, byte[] data, bool encrypt = true,
        CancellationToken cancellationToken = default)
    {
        _ = EnsureRuntimeAccount();
        return _engine.SendAsync(cmd, data, encrypt, cancellationToken);
    }

    /// <summary>
    ///     监听消息推送流
    /// </summary>
    public IAsyncEnumerable<WSRes> ListenAsync(CancellationToken cancellationToken = default)
    {
        _ = EnsureRuntimeAccount();
        return _engine.ListenAsync(cancellationToken);
    }

    /// <summary>
    ///     优雅关闭连接
    /// </summary>
    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        return _engine.CloseAsync(cancellationToken);
    }

    /// <summary>
    ///     打包原始字节流为贴吧 WebSocket 协议格式
    /// </summary>
    internal byte[] PackWsBytes(byte[] data, int cmd, int reqId, bool encrypt = true)
    {
        return _frameCodec.Pack(data, cmd, reqId, EnsureRuntimeAccount(), encrypt);
    }

    /// <summary>
    ///     解析贴吧 WebSocket 协议包，处理解密与解压
    /// </summary>
    internal (byte[] data, int cmd, int reqId) ParseWsBytes(byte[] data)
    {
        var (payload, cmd, reqId) = _frameCodec.Parse(data, EnsureRuntimeAccount());
        return (payload, cmd, reqId);
    }
}
