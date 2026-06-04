using AioTieba4DotNet.Models.Users;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class BlacklistOldUserMapper
{
    internal static BlacklistOldUser FromTbData(UserMuteQueryResIdl.Types.DataRes.Types.MuteUser data)
    {
        return new BlacklistOldUser
        {
            UserId = data.UserId,
            Portrait = UserProtoMapping.NormalizePortrait(data.Portrait),
            UserName = data.UserName,
            NickNameOld = data.NameShow,
            UntilTime = data.MuteTime
        };
    }
}
