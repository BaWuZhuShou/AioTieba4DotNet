namespace AioTieba4DotNet.Exceptions;

public class TiebaProtocolException(string message, Exception? innerException = null)
    : TiebaTransportException(message, innerException);
