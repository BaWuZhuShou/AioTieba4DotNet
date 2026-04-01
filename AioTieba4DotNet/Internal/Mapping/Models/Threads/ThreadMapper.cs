using AioTieba4DotNet.Models.Contents;
using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Models.Threads;
using ThreadModel = AioTieba4DotNet.Models.Threads.Thread;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class ThreadMapper
{
    internal static ThreadModel FromTbData(ThreadInfo? threadInfo)

    {
        if (threadInfo == null)

            return new ThreadModel
            {
                Content = ContentMapper.FromTbData((ThreadInfo?)null), VirtualImage = new VirtualImagePf()
            };


        return new ThreadModel
        {
            Content = ContentMapper.FromTbData(threadInfo),
            Title = threadInfo.Title,
            Tid = threadInfo.Id,
            Pid = threadInfo.FirstPostId,
            User = UserInfoTMapper.FromTbData(threadInfo.Author),
            AuthorId = threadInfo.AuthorId,
            VirtualImage = VirtualImagePfMapper.FromTbData(threadInfo),
            Type = threadInfo.ThreadType,
            TabId = threadInfo.TabId,
            IsGood = threadInfo.IsGood == 1,
            IsTop = threadInfo.IsTop == 1,
            IsShare = threadInfo.IsShareThread == 1 && threadInfo.OriginThreadInfo is { Pid: > 0 },
            IsHide = threadInfo.IsFrsMask == 1,
            IsLivePost = threadInfo.IsLivepost == 1,
            VoteInfo = threadInfo.PollInfo != null ? VoteInfoMapper.FromTbData(threadInfo.PollInfo) : null,
            ShareOrigin = threadInfo is { IsShareThread: 1, OriginThreadInfo.Pid: > 0 }
                ? ShareThreadMapper.FromTbData(threadInfo.OriginThreadInfo)
                : null,
            ViewNum = threadInfo.ViewNum,
            ReplyNum = threadInfo.ReplyNum,
            ShareNum = threadInfo.ShareNum,
            Agree = threadInfo.Agree?.AgreeNum ?? 0,
            Disagree = threadInfo.Agree?.DisagreeNum ?? 0,
            CreateTime = threadInfo.CreateTime,
            LastTime = threadInfo.LastTimeInt
        };
    }
}
