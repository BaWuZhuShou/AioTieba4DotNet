using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Exceptions;
using AioTieba4DotNet.Session;

namespace AioTieba4DotNet.Transport;

internal sealed class LegacyTransportContext
{
    internal LegacyTransportContext(TiebaClientSession session)
        : this(session.HttpCore, session.WsCore, session.Options.TransportMode, session)
    {
    }

    internal LegacyTransportContext(ITiebaHttpCore httpCore, ITiebaWsCore wsCore, TiebaTransportMode transportMode,
        TiebaClientSession? session = null)
    {
        HttpCore = httpCore;
        WsCore = wsCore;
        Session = session;
        TransportMode = transportMode;
        Dispatcher = new LegacyTransportDispatcher(wsCore, transportMode);
    }

    internal TiebaClientSession? Session { get; }

    internal ITiebaHttpCore HttpCore { get; }

    internal ITiebaWsCore WsCore { get; }

    internal TiebaTransportMode TransportMode { get; }

    internal LegacyTransportDispatcher Dispatcher { get; }

    internal TiebaClientSession RequireSession(string operationName) =>
        Session ?? throw new TiebaConfigurationException(
            $"Operation '{operationName}' requires a bound client session.");
}
