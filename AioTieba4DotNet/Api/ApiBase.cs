using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Enums;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api;

/// <summary>
///     所有 API 实现的基类
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
public abstract class ApiBase(ITiebaHttpCore httpCore)
{
    /// <summary>
    ///     Http 核心组件
    /// </summary>
    protected readonly ITiebaHttpCore HttpCore = httpCore;

    /// <summary>
    ///     确保 TBS 已加载（如果已登录）
    /// </summary>
    protected async Task EnsureTbsAsync()
    {
        if (HttpCore.Account != null && !string.IsNullOrEmpty(HttpCore.Account.Bduss))
            await HttpCore.GetTbsAsync();
    }

    /// <summary>
    ///     检查错误码并抛出异常
    /// </summary>
    /// <param name="code">错误码</param>
    /// <param name="msg">错误消息</param>
    /// <exception cref="TieBaServerException">当错误码不为 0 时抛出</exception>
    protected static void CheckError(int code, string? msg)
    {
        if (code != 0) throw new TieBaServerException(code, msg ?? string.Empty);
    }

    /// <summary>
    ///     解析 JSON 响应体并检查错误码
    /// </summary>
    /// <param name="body">响应字符串</param>
    /// <param name="codeField">错误码字段名</param>
    /// <param name="msgField">错误消息字段名</param>
    /// <returns>解析后的 JObject</returns>
    protected static JObject ParseBody(string body, string codeField = "error_code", string msgField = "error_msg")
    {
        var resJson = JObject.Parse(body);
        var code = resJson.GetValue(codeField)?.ToObject<int>() ?? 0;
        var msg = resJson.GetValue(msgField)?.ToObject<string>() ?? string.Empty;
        CheckError(code, msg);
        return resJson;
    }
}

/// <summary>
///     支持双模（Http/Websocket）分发的 API 基类
/// </summary>
/// <typeparam name="TResult">响应实体类型</typeparam>
/// <param name="httpCore">Http 核心组件</param>
/// <param name="wsCore">Websocket 核心组件</param>
/// <param name="mode">请求模式</param>
public abstract class ApiWsBase<TResult>(
    ITiebaHttpCore httpCore,
    ITiebaWsCore wsCore,
    TiebaRequestMode mode = TiebaRequestMode.Http)
    : ApiBase(httpCore)
{
    /// <summary>
    ///     请求模式
    /// </summary>
    protected readonly TiebaRequestMode Mode = mode;

    /// <summary>
    ///     Websocket 核心组件
    /// </summary>
    protected readonly ITiebaWsCore WsCore = wsCore;

    /// <summary>
    ///     根据当前模式执行请求分发
    /// </summary>
    /// <param name="httpRequest">Http 执行逻辑</param>
    /// <param name="wsRequest">Websocket 执行逻辑（可选）</param>
    /// <returns>API 响应结果</returns>
    protected async Task<TResult> ExecuteAsync(Func<Task<TResult>> httpRequest, Func<Task<TResult>>? wsRequest = null)
    {
        await EnsureTbsAsync();

        if (Mode != TiebaRequestMode.Websocket || wsRequest == null) return await httpRequest();
        try
        {
            return await wsRequest();
        }
        catch (NotImplementedException)
        {
            // 强制要求ws但是未实现，回退http
        }

        return await httpRequest();
    }
}
