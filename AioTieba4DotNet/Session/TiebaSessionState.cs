namespace AioTieba4DotNet.Session;

internal enum TiebaSessionKind
{
    Guest,
    Authenticated
}

internal enum TiebaSessionResourceState
{
    Unavailable,
    Pending,
    Initializing,
    Ready
}

internal sealed record TiebaSessionState(
    TiebaSessionKind Kind,
    TiebaSessionResourceState TbsState,
    string? Tbs,
    TiebaSessionResourceState ClientState,
    string? ClientId,
    string? SampleId,
    TiebaSessionResourceState ZIdState,
    string? ZId,
    TiebaSessionResourceState WebSocketState)
{
    internal bool IsAuthenticated => Kind == TiebaSessionKind.Authenticated;

    internal static TiebaSessionState FromAccount(Account? account)
    {
        if (account == null || string.IsNullOrWhiteSpace(account.Bduss))
            return new TiebaSessionState(
                TiebaSessionKind.Guest,
                TiebaSessionResourceState.Unavailable,
                null,
                TiebaSessionResourceState.Unavailable,
                null,
                null,
                TiebaSessionResourceState.Unavailable,
                null,
                TiebaSessionResourceState.Pending);

        return new TiebaSessionState(
            TiebaSessionKind.Authenticated,
            ToValueState(account.Tbs),
            account.Tbs,
            ToClientState(account.ClientId, account.SampleId),
            account.ClientId,
            account.SampleId,
            ToValueState(account.ZId),
            account.ZId,
            TiebaSessionResourceState.Pending);
    }

    private static TiebaSessionResourceState ToValueState(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? TiebaSessionResourceState.Pending
            : TiebaSessionResourceState.Ready;
    }

    private static TiebaSessionResourceState ToClientState(string? clientId, string? sampleId)
    {
        return string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(sampleId)
            ? TiebaSessionResourceState.Pending
            : TiebaSessionResourceState.Ready;
    }
}
