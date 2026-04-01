namespace AioTieba4DotNet;

/// <summary>
///     鉴权异常
/// </summary>
/// <param name="message">异常消息</param>
public class TiebaAuthenticationException(string message) : TiebaException(message);
