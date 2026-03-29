using AioTieba4DotNet.Modules;
using AioTieba4DotNet.Protocols;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet;

internal static class TiebaClientComposition
{
    internal static TiebaClientRuntime CreateRuntime(TiebaOptions options, HttpClient? httpClient = null)
    {
        var session = new TiebaClientSession(options, httpClient);
        var transport = new LegacyTransportContext(session);
        var forumCache = new ForumInfoCache();
        var forumProtocol = new LegacyForumProtocol(transport, forumCache);
        var threadProtocol = new LegacyThreadProtocol(transport, forumProtocol);
        var userProtocol = new LegacyUserProtocol(transport, forumProtocol);
        var clientProtocol = new LegacyClientProtocol(transport);

        return new TiebaClientRuntime(
            session,
            new ForumModule(forumProtocol),
            new ThreadModule(threadProtocol),
            new UserModule(userProtocol),
            new ClientModule(clientProtocol));
    }
}

internal sealed record TiebaClientRuntime(
    TiebaClientSession Session,
    IForumModule Forums,
    IThreadModule Threads,
    IUserModule Users,
    IClientModule Client);
