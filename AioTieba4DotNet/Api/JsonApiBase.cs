using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api;

/// <summary>
/// 处理 JSON 格式响应的 API 基类
/// </summary>
/// <param name="httpCore"></param>
public abstract class JsonApiBase(ITiebaHttpCore httpCore) : ApiBase(httpCore)
{
    /// <summary>
    /// 解析 JSON 响应体并检查错误码
    /// </summary>
    /// <param name="body">响应字符串</param>
    /// <returns>解析后的 JObject</returns>
    /// <exception cref="TieBaServerException">当 error_code 不为 0 时抛出</exception>
    protected static JObject ParseBody(string body)
    {
        return ParseBody(body, "error_code", "error_msg");
    }

    /// <summary>
    /// 解析 JSON 响应体并检查错误码
    /// </summary>
    /// <param name="body">响应字符串</param>
    /// <param name="codeField">错误码字段名</param>
    /// <param name="msgField">错误消息字段名</param>
    /// <returns>解析后的 JObject</returns>
    /// <exception cref="TieBaServerException">当错误码不为 0 时抛出</exception>
    protected static JObject ParseBody(string body, string codeField, string msgField)
    {
        var resJson = JObject.Parse(body);
        var code = resJson.GetValue(codeField)?.ToObject<int>() ?? 0;
        if (code != 0)
        {
            throw new TieBaServerException(code, resJson.GetValue(msgField)?.ToObject<string>() ?? string.Empty);
        }
        return resJson;
    }
}
