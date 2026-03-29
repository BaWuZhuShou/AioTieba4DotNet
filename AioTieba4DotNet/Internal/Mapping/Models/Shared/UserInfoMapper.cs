using AioTieba4DotNet.Models.Shared;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class UserInfoMapper
{
    internal static UserInfo? FromTbData(PostInfoList? dataRes)
    {
        if (dataRes == null) return null;

        return new UserInfo
        {
            UserId = dataRes.UserId,
            Portrait = NormalizePortrait(dataRes.UserPortrait),
            UserName = dataRes.UserName,
            NickNameNew = dataRes.NameShow
        };
    }

    internal static UserInfo FromTbData(JObject data)
    {
        return new UserInfo
        {
            UserId = data.GetValue("id")?.Value<long>() ?? 0,
            Portrait = NormalizePortrait(data.GetValue("portrait")?.Value<string>() ?? string.Empty),
            UserName = data.GetValue("name")?.Value<string>() ?? string.Empty,
            NickNameNew = data.GetValue("name_show")?.Value<string>() ?? string.Empty
        };
    }

    internal static UserInfo? FromTbData(User? data)
    {
        if (data == null) return null;

        return new UserInfo
        {
            UserId = data.Id,
            Portrait = NormalizePortrait(data.Portrait ?? string.Empty),
            UserName = data.Name,
            NickNameNew = data.NameShow
        };
    }

    private static string NormalizePortrait(string portrait)
    {
        if (portrait.Contains('?'))
        {
            portrait = portrait[..^13];
        }

        return portrait;
    }
}
