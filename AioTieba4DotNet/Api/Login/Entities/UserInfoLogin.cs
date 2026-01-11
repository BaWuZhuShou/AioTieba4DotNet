using AioTieba4DotNet.Entities;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.Login.Entities;

/// <summary>
///     登录用户信息
/// </summary>
public class UserInfoLogin : UserInfo
{
    /// <summary>
    ///     从贴吧原始数据转换
    /// </summary>
    /// <param name="data">JSON 数据对象</param>
    /// <returns>用户信息实体</returns>
    public new static UserInfoLogin FromTbData(JObject data)
    {
        var portrait = data.GetValue("portrait")?.Value<string>() ?? "";
        if (portrait.Contains('?')) portrait = portrait[..^13];

        return new UserInfoLogin
        {
            UserId = data.GetValue("id")?.Value<long>() ?? 0,
            Portrait = portrait,
            UserName = data.GetValue("name")?.Value<string>() ?? ""
        };
    }
}
