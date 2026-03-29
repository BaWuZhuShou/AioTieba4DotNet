namespace AioTieba4DotNet.Exceptions;

public class TiebaTransportException : TiebaException
{
    public TiebaTransportException(string message) : base(message)
    {
    }

    public TiebaTransportException(string message, Exception? innerException) : base(message, innerException)
    {
    }
}
