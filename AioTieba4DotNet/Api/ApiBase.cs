using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Enums;

namespace AioTieba4DotNet.Api;

/// <summary>
/// 所有 API 实现的基类
/// </summary>
/// <param name="httpCore"></param>
public abstract class ApiBase(ITiebaHttpCore httpCore)
{
    protected readonly ITiebaHttpCore HttpCore = httpCore;
}

/// <summary>
/// 支持双模（Http/Websocket）分发的 API 基类
/// </summary>
/// <typeparam name="TResult">响应实体类型</typeparam>
/// <param name="httpCore"></param>
/// <param name="wsCore"></param>
/// <param name="mode"></param>
public abstract class ApiWsBase<TResult>(
    ITiebaHttpCore httpCore,
    ITiebaWsCore wsCore,
    TiebaRequestMode mode = TiebaRequestMode.Http)
    : ApiBase(httpCore)
{
    protected readonly ITiebaWsCore WsCore = wsCore;
    protected readonly TiebaRequestMode Mode = mode;

    /// <summary>
    /// 根据当前模式执行请求分发
    /// </summary>
    /// <param name="httpRequest">Http 执行逻辑</param>
    /// <param name="wsRequest">Websocket 执行逻辑（可选）</param>
    /// <returns></returns>
    protected async Task<TResult> ExecuteAsync(Func<Task<TResult>> httpRequest, Func<Task<TResult>>? wsRequest = null)
    {
        if (Mode == TiebaRequestMode.Websocket && wsRequest != null)
        {
            try
            {
                return await wsRequest();
            }
            catch (NotImplementedException)
            {
                // 强制要求ws但是未实现，回退http
            }
        }

        return await httpRequest();
    }
}
