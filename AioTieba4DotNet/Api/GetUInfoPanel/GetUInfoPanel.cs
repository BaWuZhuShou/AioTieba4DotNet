using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Transport;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetUInfoPanel;

/// <summary>
///     获取用户面板信息的 API (Web端接口，常用于通过用户名换取 Portrait)
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
[PythonApi("aiotieba.api.get_uinfo_panel")]
internal class GetUInfoPanel(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static UserInfo ParseBody(string body)
    {
        var o = ParseBody(body, "no", "error");

        var data = o.GetValue("data")?.ToObject<JObject>();
        if (data == null) throw new TieBaServerException(-1, "无法获取到用户数据!");

        return UserInfoPanelMapper.FromTbData(data);
    }

    /// <summary>
    ///     发送获取用户面板信息请求
    /// </summary>
    /// <param name="nameOrPortrait">用户名 (un) 或用户头像 ID (portrait)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户面板信息</returns>
    public async Task<UserInfo> RequestAsync(string nameOrPortrait,
        CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            Utils.IsPortrait(nameOrPortrait)
                ? new KeyValuePair<string, string>("id", nameOrPortrait)
                : new KeyValuePair<string, string>("un", nameOrPortrait)
        };
        var requestUri = new UriBuilder("https", Const.WebBaseHost, 443, "/home/get/panel").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        return ParseBody(result);
    }
}
