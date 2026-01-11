namespace AioTieba4DotNet.Exceptions;

/// <summary>
///     贴吧客户端异常
/// </summary>
/// <param name="message">异常信息</param>
public class TiebaException(string message) : Exception(message);
