using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api;

internal static class ApiResponseValidator
{
    internal static void CheckError(int code, string? msg)
    {
        if (code != 0)
            throw new TieBaServerException(code, msg ?? string.Empty);
    }

    internal static JObject ParseJsonBody(string body, string codeField = "error_code", string msgField = "error_msg")
    {
        var responseJson = JObject.Parse(body);
        var code = responseJson.GetValue(codeField)?.ToObject<int>() ?? 0;
        var message = responseJson.GetValue(msgField)?.ToObject<string>() ?? string.Empty;
        CheckError(code, message);
        return responseJson;
    }
}
