using AioTieba4DotNet.Models.Shared;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class UserInfoSelfInitMapper
{
    internal static UserInfo FromTbData(JObject data)
    {
        return new UserInfo
        {
            UserName = data.GetValue("user_name")?.Value<string>() ?? string.Empty,
            NickNameOld = data.GetValue("name_show")?.Value<string>() ?? string.Empty,
            TiebaUid = data.GetValue("tieba_uid")?.Value<long>() ?? 0
        };
    }
}
