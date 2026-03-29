namespace AioTieba4DotNet.Exceptions;

/// <summary>
///     传输异常
/// </summary>
public class TiebaTransportException : TiebaException
{
    /// <summary>
    ///     初始化异常
    /// </summary>
    /// <param name="message">异常消息</param>
    public TiebaTransportException(string message) : base(message)
    {
    }

    /// <summary>
    ///     初始化异常
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public TiebaTransportException(string message, Exception? innerException) : base(message, innerException)
    {
    }
}
