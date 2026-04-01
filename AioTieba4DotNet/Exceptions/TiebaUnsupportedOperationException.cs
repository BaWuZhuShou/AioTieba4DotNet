namespace AioTieba4DotNet;

/// <summary>
///     当前客户端不支持该操作路径。
/// </summary>
public class TiebaUnsupportedOperationException : TiebaException
{
    /// <summary>
    ///     初始化不受支持的操作异常。
    /// </summary>
    /// <param name="message">异常消息。</param>
    /// <param name="innerException">内部异常。</param>
    public TiebaUnsupportedOperationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
