namespace AioTieba4DotNet;

public interface ITiebaClient : IDisposable
{
    IForumModule Forums { get; }

    IThreadModule Threads { get; }

    IUserModule Users { get; }

    IClientModule Client { get; }
}
