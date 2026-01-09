using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Enums;
using AioTieba4DotNet.Exceptions;

namespace AioTieba4DotNet.Api;

/// <summary>
/// 处理 Protobuf 格式响应的 API 基类
/// </summary>
/// <param name="httpCore"></param>
public abstract class ProtoApiBase(ITiebaHttpCore httpCore) : ApiBase(httpCore)
{
    /// <summary>
    /// 检查 Protobuf 响应中的错误码
    /// </summary>
    /// <param name="code">错误码</param>
    /// <param name="msg">错误消息</param>
    /// <exception cref="TieBaServerException">当 code 不为 0 时抛出</exception>
    protected static void CheckError(int code, string? msg)
    {
        if (code != 0)
        {
            throw new TieBaServerException(code, msg ?? string.Empty);
        }
    }
}

/// <summary>
/// 支持双模分发的 Protobuf API 基类
/// </summary>
/// <typeparam name="TResult"></typeparam>
/// <param name="httpCore"></param>
/// <param name="wsCore"></param>
/// <param name="mode"></param>
public abstract class ProtoApiWsBase<TResult>(ITiebaHttpCore httpCore, ITiebaWsCore wsCore, TiebaRequestMode mode = TiebaRequestMode.Http) 
    : ApiWsBase<TResult>(httpCore, wsCore, mode)
{
    /// <summary>
    /// 检查 Protobuf 响应中的错误码
    /// </summary>
    /// <param name="code">错误码</param>
    /// <param name="msg">错误消息</param>
    /// <exception cref="TieBaServerException">当 code 不为 0 时抛出</exception>
    protected static void CheckError(int code, string? msg)
    {
        if (code != 0)
        {
            throw new TieBaServerException(code, msg ?? string.Empty);
        }
    }
}
