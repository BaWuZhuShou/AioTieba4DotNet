using AioTieba4DotNet.Models.Users;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class BlacklistOldUserMapper
{
    internal static BlacklistOldUser FromTbData(UserMuteQueryResIdl.Types.DataRes.Types.MuteUser data)
    {
        var portrait = data.Portrait;
        if (portrait.Contains('?', StringComparison.Ordinal))
            portrait = portrait[..portrait.IndexOf('?', StringComparison.Ordinal)];

        return new BlacklistOldUser
        {
            UserId = data.UserId,
            Portrait = portrait,
            UserName = data.UserName,
            NickNameOld = data.NameShow,
            UntilTime = data.MuteTime
        };
    }
}
