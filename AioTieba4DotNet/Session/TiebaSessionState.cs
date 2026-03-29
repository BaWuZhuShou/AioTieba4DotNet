using AioTieba4DotNet.Core;

namespace AioTieba4DotNet.Session;

internal enum TiebaSessionKind
{
    Guest,
    Authenticated
}

internal sealed record TiebaSessionState(
    TiebaSessionKind Kind,
    string? Tbs,
    string? ClientId,
    string? SampleId,
    string? ZId)
{
    internal bool IsAuthenticated => Kind == TiebaSessionKind.Authenticated;

    internal static TiebaSessionState FromAccount(Account? account)
    {
        if (account == null || string.IsNullOrWhiteSpace(account.Bduss)) return new(TiebaSessionKind.Guest, null, null, null, null);

        return new(
            TiebaSessionKind.Authenticated,
            account.Tbs,
            account.ClientId,
            account.SampleId,
            account.ZId);
    }
}
