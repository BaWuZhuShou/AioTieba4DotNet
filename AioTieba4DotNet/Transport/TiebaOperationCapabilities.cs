namespace AioTieba4DotNet.Transport;

internal readonly record struct TiebaOperationCapabilities(
    TiebaOperationTransportKind TransportKind,
    bool RequiresAuthentication = false,
    bool RequiresTbs = false)
{
    internal static TiebaOperationCapabilities HttpOnly(bool requiresAuthentication = false, bool requiresTbs = false) =>
        new(TiebaOperationTransportKind.HttpOnly, requiresAuthentication, requiresTbs);

    internal static TiebaOperationCapabilities WebSocketPreferred(bool requiresAuthentication = false,
        bool requiresTbs = false) =>
        new(TiebaOperationTransportKind.WebSocketPreferred, requiresAuthentication, requiresTbs);

    internal static TiebaOperationCapabilities WebSocketOnly(bool requiresAuthentication = false) =>
        new(TiebaOperationTransportKind.WebSocketOnly, requiresAuthentication, false);
}
