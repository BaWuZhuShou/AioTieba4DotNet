namespace AioTieba4DotNet.Exceptions;

/// <summary>
///     配置异常
/// </summary>
/// <param name="message">异常消息</param>
public class TiebaConfigurationException(string message) : TiebaException(message);
