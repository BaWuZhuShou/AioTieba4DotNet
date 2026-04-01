using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Threads;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class UserInfoTMapper
{
    internal static UserInfoT? FromTbData(User? dataProto)

        {

            if (dataProto == null) return null;



            var portrait = dataProto.Portrait ?? "";

            if (portrait.Contains('?')) portrait = portrait[..^13];



            return new UserInfoT

            {

                UserId = dataProto.Id,

                Portrait = portrait,

                UserName = dataProto.Name,

                NickNameNew = dataProto.NameShow,

                Level = dataProto.LevelId,

                GLevel = (int)(dataProto.UserGrowth?.LevelId ?? 0),

                Gender = (Gender)dataProto.Gender,

                Icons = dataProto.Iconinfo?.Where(i => !string.IsNullOrEmpty(i.Name)).Select(i => i.Name).ToList() ?? [],

                IsBawu = dataProto.IsBawu == 1,

                IsVip = dataProto.NewTshowIcon.Count != 0,

                IsGod = dataProto.NewGodData is { Status: 1 },

                PrivLike = dataProto.PrivSets != null && dataProto.PrivSets.Like != 0

                    ? (PrivLike)dataProto.PrivSets.Like

                    : PrivLike.Public,

                PrivReply = dataProto.PrivSets != null && dataProto.PrivSets.Reply != 0

                    ? (PrivReply)dataProto.PrivSets.Reply

                    : PrivReply.All,

                Ip = dataProto.IpAddress

            };

        }
}
