using AioTieba4DotNet.Models.Forums;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class ForumLevelInfoMapper
{
    internal static ForumLevelInfo FromTbData(GetLevelInfoResIdl.Types.DataRes data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return new ForumLevelInfo
        {
            LevelName = data.LevelName,
            UserLevel = data.UserLevel,
            IsLike = data.IsLike != 0
        };
    }
}
