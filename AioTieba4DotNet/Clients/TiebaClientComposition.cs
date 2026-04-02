using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Modules;
using AioTieba4DotNet.Protocols;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet;

internal sealed class TiebaClientComposition
{
    private readonly Func<HttpClient?> _createHttpClient;

    private TiebaClientComposition(Func<HttpClient?> createHttpClient)
    {
        _createHttpClient = createHttpClient;
    }

    internal static TiebaClientComposition Direct { get; } = new(static () => null);

    internal static TiebaClientComposition CreateForDependencyInjection(IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        return new TiebaClientComposition(() => httpClientFactory.CreateClient(DependencyInjection.HttpClientName));
    }

    internal TiebaClient CreateClient(TiebaOptions options)
    {
        return new TiebaClient(CreateRuntime(options));
    }

    internal TiebaClientRuntime CreateRuntime(TiebaOptions options)
    {
        TiebaOptionsValidator.Validate(options);

        var session = new TiebaClientSession(options, _createHttpClient());
        var dispatcher = new TiebaOperationDispatcher(session);
        var forumCache = new ForumInfoCache();
        var adminProtocol = new AdminProtocol(dispatcher, forumCache);
        var forumProtocol = new ForumProtocol(dispatcher, forumCache);
        var threadProtocol = new ThreadProtocol(dispatcher, forumProtocol);
        var userProtocol = new UserProtocol(dispatcher, forumProtocol);
        var messagesProtocol = new MessagesProtocol(dispatcher, userProtocol);
        var clientProtocol = new ClientProtocol(dispatcher);

        return new TiebaClientRuntime(
            session,
            new ForumModule(forumProtocol),
            new ThreadModule(threadProtocol),
            new UserModule(userProtocol),
            new AdminModule(adminProtocol),
            new MessagesModule(messagesProtocol),
            new ClientModule(clientProtocol));
    }
}

internal sealed record TiebaClientRuntime(
    TiebaClientSession Session,
    IForumModule Forums,
    IThreadModule Threads,
    IUserModule Users,
    IAdminModule Admins,
    IMessagesModule Messages,
    IClientModule Client);
