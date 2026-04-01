namespace AioTieba4DotNet;

/// <summary>
///     协议异常
/// </summary>
public class TiebaProtocolException : TiebaException
{
    /// <summary>
    ///     初始化协议异常。
    /// </summary>
    /// <param name="message">异常消息。</param>
    /// <param name="innerException">内部异常。</param>
    public TiebaProtocolException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
