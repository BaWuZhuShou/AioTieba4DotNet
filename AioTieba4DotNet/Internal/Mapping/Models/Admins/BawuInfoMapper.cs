using AioTieba4DotNet.Models.Admins;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class BawuInfoMapper
{
    internal static BawuInfo FromTbData(GetBawuInfoResIdl.Types.DataRes data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var all = new List<BawuUser>();
        var roles = data.BawuTeamInfo?.BawuTeamList ?? [];
        var roleMap = roles.ToDictionary(
            static role => role.RoleName,
            static role => role.RoleInfo.Select(static user => new BawuUser
            {
                UserId = user.UserId,
                Portrait = user.Portrait,
                UserName = user.UserName,
                NickNameNew = user.NameShow,
                Level = user.UserLevel
            }).ToList());

        IReadOnlyList<BawuUser> Extract(string roleName)
        {
            if (!roleMap.TryGetValue(roleName, out var users))
                return [];

            all.AddRange(users);
            return users;
        }

        return new BawuInfo
        {
            All = all,
            Admins = Extract("吧主"),
            Managers = Extract("小吧主"),
            VoiceEditors = Extract("语音小编"),
            ImageEditors = Extract("图片小编"),
            VideoEditors = Extract("视频小编"),
            BroadcastEditors = Extract("广播小编"),
            JournalChiefEditors = Extract("吧刊主编"),
            JournalEditors = Extract("吧刊小编"),
            ProfessAdmins = Extract("职业吧主"),
            FourthAdmins = Extract("第四吧主")
        };
    }
}
