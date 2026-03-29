using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Contents;
using AioTieba4DotNet.Models.Users;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class UserThreadMapper
{
    internal static UserThread FromTbData(PostInfoList dataRes)

        {

            return new UserThread

            {

                Contents = AioTieba4DotNet.Internal.Mapping.ContentMapper.FromTbData(dataRes),

                Title = dataRes.Title,

                Fid = (long)dataRes.ForumId,

                Fname = dataRes.ForumName,

                Tid = (long)dataRes.ThreadId,

                Pid = (long)dataRes.PostId,

                Type = (int)dataRes.ThreadType,

                VoteInfo = AioTieba4DotNet.Internal.Mapping.VoteInfoMapper.FromTbData(dataRes.PollInfo),

                ViewNum = dataRes.FreqNum,

                ReplyNum = (int)dataRes.ReplyNum,

                ShareNum = dataRes.ShareNum,

                Agree = dataRes.Agree?.AgreeNum ?? 0,

                Disagree = dataRes.Agree?.DisagreeNum ?? 0,

                CreateTime = (int)dataRes.CreateTime

            };

        }
}
