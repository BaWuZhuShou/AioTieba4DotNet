using AioTieba4DotNet.Models.Users;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class UserInfoTUidMapper
{
    internal static UserInfoTUid FromTbData(User data)
    {
        var portrait = data.Portrait;
        if (portrait.Contains('?', StringComparison.Ordinal))
            portrait = portrait[..portrait.IndexOf('?', StringComparison.Ordinal)];

        return new UserInfoTUid
        {
            UserId = data.Id,
            Portrait = portrait,
            UserName = data.Name,
            NickNameNew = data.NameShow,
            TiebaUid = string.IsNullOrEmpty(data.TiebaUid) ? 0 : long.Parse(data.TiebaUid),
            Age = string.IsNullOrEmpty(data.TbAge) ? 0 : float.Parse(data.TbAge),
            Sign = data.Intro,
            IsGod = data.NewGodData?.Status == 1
        };
    }
}
