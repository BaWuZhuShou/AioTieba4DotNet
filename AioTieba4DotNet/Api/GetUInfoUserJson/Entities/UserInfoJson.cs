using AioTieba4DotNet.Entities;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetUInfoUserJson.Entities;

/// <summary>
///     用户信息 (JSON 接口)
/// </summary>
public class UserInfoJson : UserInfo
{
    /// <summary>
    ///     从贴吧原始数据转换
    /// </summary>
    /// <param name="data">JSON 数据对象</param>
    /// <returns>用户信息实体</returns>
    public new static UserInfoJson FromTbData(JObject data)
    {
        var portrait = data["portrait"]?.ToString() ?? "";
        if (portrait.Contains('?')) portrait = portrait[..^13];

        return new UserInfoJson
        {
            UserId = data["id"]?.ToObject<long>() ?? 0,
            Portrait = portrait,
            UserName = data["name"]?.ToString() ?? "",
            NickNameNew = data["name_show"]?.ToString() ?? ""
        };
    }
}
