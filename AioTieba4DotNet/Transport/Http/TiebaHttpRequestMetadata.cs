using AioTieba4DotNet.Session;

namespace AioTieba4DotNet.Transport.Http;

internal static class TiebaHttpRequestMetadata
{
    private static readonly HttpRequestOptionsKey<TiebaHttpRequestKind> RequestKindKey = new("AioTieba4DotNet.Transport.Http.RequestKind");
    private static readonly HttpRequestOptionsKey<Account> AccountKey = new("AioTieba4DotNet.Transport.Http.Account");

    internal static void Apply(HttpRequestMessage request, TiebaHttpRequestKind requestKind, Account? account)
    {
        ArgumentNullException.ThrowIfNull(request);

        request.Options.Set(RequestKindKey, requestKind);
        if (account != null)
            request.Options.Set(AccountKey, account);
    }

    internal static TiebaHttpRequestKind? TryGetRequestKind(HttpRequestMessage request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Options.TryGetValue(RequestKindKey, out TiebaHttpRequestKind requestKind)
            ? requestKind
            : null;
    }

    internal static Account? TryGetAccount(HttpRequestMessage request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Options.TryGetValue(AccountKey, out Account? account)
            ? account
            : null;
    }
}
