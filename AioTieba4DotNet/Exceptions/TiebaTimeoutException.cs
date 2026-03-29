namespace AioTieba4DotNet.Exceptions;

/// <summary>
///     超时异常
/// </summary>
/// <param name="message">异常消息</param>
/// <param name="innerException">内部异常</param>
public class TiebaTimeoutException(string message, Exception? innerException = null)
    : TiebaTransportException(message, innerException);
