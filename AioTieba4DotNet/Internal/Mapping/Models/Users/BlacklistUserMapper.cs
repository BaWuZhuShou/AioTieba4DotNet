using AioTieba4DotNet.Models.Users;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class BlacklistUserMapper
{
    internal static BlacklistUser FromTbData(JObject data)
    {
        var user = UserInfoMapper.FromTbData(data);
        var permList = data.GetValue("perm_list") as JObject;

        return new BlacklistUser
        {
            UserId = user.UserId,
            Portrait = user.Portrait,
            UserName = user.UserName,
            NickNameNew = user.NickNameNew,
            BlockFollow = permList?.GetValue("follow")?.Value<int>() == 1,
            BlockInteract = permList?.GetValue("interact")?.Value<int>() == 1,
            BlockChat = permList?.GetValue("chat")?.Value<int>() == 1
        };
    }
}
