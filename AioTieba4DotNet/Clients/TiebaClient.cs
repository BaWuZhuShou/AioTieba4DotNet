using AioTieba4DotNet.Modules;

namespace AioTieba4DotNet;

public sealed class TiebaClient : ITiebaClient
{
    private readonly IDisposable _lifetime;

    public TiebaClient(string? bduss = null, string? stoken = null)
        : this(new TiebaOptions { Bduss = bduss, Stoken = stoken })
    {
    }

    public TiebaClient(TiebaOptions options)
        : this(TiebaClientComposition.CreateRuntime(options))
    {
    }

    internal TiebaClient(TiebaClientRuntime runtime)
    {
        _lifetime = runtime.Session;
        Forums = runtime.Forums;
        Threads = runtime.Threads;
        Users = runtime.Users;
        Client = runtime.Client;
    }

    public IForumModule Forums { get; }

    public IThreadModule Threads { get; }

    public IUserModule Users { get; }

    public IClientModule Client { get; }

    public void Dispose()
    {
        _lifetime.Dispose();
    }
}
