namespace AioTieba4DotNet.Exceptions;

/// <summary>
///     贴吧客户端异常
/// </summary>
public class TiebaException : Exception
{
    public TiebaException(string message) : base(message)
    {
    }

    public TiebaException(string message, Exception? innerException) : base(message, innerException)
    {
    }
}
