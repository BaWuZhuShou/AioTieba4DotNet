namespace AioTieba4DotNet.Exceptions;

public class TiebaTimeoutException(string message, Exception? innerException = null)
    : TiebaTransportException(message, innerException);
