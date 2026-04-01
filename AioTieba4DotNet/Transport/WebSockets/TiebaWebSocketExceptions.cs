namespace AioTieba4DotNet.Transport.WebSockets;

internal sealed class TiebaWebSocketUnavailableException(string message, Exception? innerException = null)
    : TiebaTransportException(message, innerException);

internal sealed class TiebaWebSocketConnectionLostException(string message, Exception? innerException = null)
    : TiebaTransportException(message, innerException);
