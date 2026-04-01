using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api;

/// <summary>
///     处理 JSON 格式响应的 API 基类
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
internal abstract class JsonApiBase(ITiebaHttpCore httpCore)
{
    protected readonly ITiebaHttpCore HttpCore = httpCore;

    protected static Newtonsoft.Json.Linq.JObject ParseBody(string body, string codeField = "error_code",
        string msgField = "error_msg")
    {
        return ApiResponseValidator.ParseJsonBody(body, codeField, msgField);
    }
}
