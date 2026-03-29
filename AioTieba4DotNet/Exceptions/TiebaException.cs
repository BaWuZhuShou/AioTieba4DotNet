namespace AioTieba4DotNet.Exceptions;

/// <summary>
///     贴吧客户端异常
/// </summary>
public class TiebaException : Exception
{
    /// <summary>
    ///     初始化异常
    /// </summary>
    /// <param name="message">异常消息</param>
    public TiebaException(string message) : base(message)
    {
    }

    /// <summary>
    ///     初始化异常
    /// </summary>
    /// <param name="message">异常消息</param>
    /// <param name="innerException">内部异常</param>
    public TiebaException(string message, Exception? innerException) : base(message, innerException)
    {
    }
}
