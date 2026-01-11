using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.Block;

/// <summary>
/// 封禁用户的 API
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
[RequireBduss]
[PythonApi("aiotieba.api.block")]
public class Block(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static bool ParseBody(string body)
    {
        JsonApiBase.ParseBody(body);
        return true;
    }

    /// <summary>
    /// 发送封禁用户请求
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <param name="portrait">用户头像 ID (Portrait)</param>
    /// <param name="day">封禁天数 (通常为 1, 3, 10)</param>
    /// <param name="reason">封禁原因</param>
    /// <returns>操作是否成功</returns>
    public async Task<bool> RequestAsync(ulong fid, string portrait, int day, string reason)
    {
        var specialDays = new HashSet<int> { 1, 3, 10 }; // 可以随时在这里添加新的特殊天数
        var isSvipBlock = specialDays.Contains(day) ? "0" : "1";
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account!.Bduss),
            new("day", day.ToString()),
            new("fid", fid.ToString()),
            new("is_loop_ban", isSvipBlock),
            new("ntn", "banid"),
            new("portrait", portrait),
            new("reason", reason),
            new("tbs", HttpCore.Account!.Tbs!),
            new("word", "-"),
            new("z", "6")
        };
        var requestUri = new UriBuilder(Const.AppSecureScheme, Const.AppBaseHost, 443, "/c/c/bawu/commitprison").Uri;

        var result = await HttpCore.SendAppFormAsync(requestUri, data);
        return ParseBody(result);
    }
}
