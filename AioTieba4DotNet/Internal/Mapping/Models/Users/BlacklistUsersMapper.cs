using AioTieba4DotNet.Models.Users;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class BlacklistUsersMapper
{
    internal static BlacklistUsers FromTbData(JObject data)
    {
        var objs = data.GetValue("user_perm_list") is JArray blacklist
            ? blacklist.OfType<JObject>().Select(BlacklistUserMapper.FromTbData).ToList()
            : [];

        return new BlacklistUsers(objs);
    }
}
