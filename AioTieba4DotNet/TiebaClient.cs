using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Modules;
using AioTieba4DotNet.Enums;

namespace AioTieba4DotNet;

public class TiebaClient : ITiebaClient
{
    public TiebaRequestMode RequestMode 
    { 
        get => Threads.RequestMode; 
        set
        {
            Threads.RequestMode = value;
            Users.RequestMode = value;
        }
    }
    public ITiebaHttpCore HttpCore { get; }
    public IForumModule Forums { get; }
    public IThreadModule Threads { get; }
    public IUserModule Users { get; }
    public IClientModule Client { get; }
    public ITiebaWsCore WsCore { get; }

    public TiebaClient(string? bduss = null, string? stoken = null) : this(new TiebaOptions { Bduss = bduss, Stoken = stoken })
    {
    }

    public TiebaClient(TiebaOptions options) : this(CreateHttpCore(options))
    {
        RequestMode = options.RequestMode;
    }

    private static ITiebaHttpCore CreateHttpCore(TiebaOptions options)
    {
        var httpCore = new HttpCore();
        if (!string.IsNullOrEmpty(options.Bduss))
        {
            httpCore.SetAccount(new Account(options.Bduss, options.Stoken ?? string.Empty));
        }
        return httpCore;
    }

    public TiebaClient(ITiebaHttpCore httpCore)
    {
        HttpCore = httpCore;
        WsCore = new WebsocketCore(httpCore.Account);
        Forums = new ForumModule(httpCore);
        Threads = new ThreadModule(httpCore, Forums, WsCore);
        Users = new UserModule(httpCore, Forums, WsCore);
        Client = new ClientModule(httpCore);
    }

    public TiebaClient(ITiebaHttpCore httpCore, IForumModule forums, IThreadModule threads, IUserModule users, IClientModule client, ITiebaWsCore wsCore)
    {
        HttpCore = httpCore;
        Forums = forums;
        Threads = threads;
        Users = users;
        Client = client;
        WsCore = wsCore;
    }
}
