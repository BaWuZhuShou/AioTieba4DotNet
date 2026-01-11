namespace AioTieba4DotNet.Exceptions;

/// <summary>
///     贴吧服务端异常
/// </summary>
/// <param name="code">错误码</param>
/// <param name="msg">错误信息</param>
public class TieBaServerException(int code, string msg) : Exception($"Code: {code}, Message: {msg}")
{
    /// <summary>
    ///     错误码
    /// </summary>
    public int Code { get; } = code;
}
