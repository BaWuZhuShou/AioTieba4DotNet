using AioTieba4DotNet.Models.Threads;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class ShareThreadMapper
{
    internal static ShareThread FromTbData(ThreadInfo.Types.OriginThreadInfo threadInfo)

    {
        return new ShareThread
        {
            Content = ContentMapper.FromTbData(threadInfo),
            AuthorId = threadInfo.Content.Count != 0 ? threadInfo.Content[0].Uid : 0,
            Title = threadInfo.Title,
            Fid = threadInfo.Fid,
            Fname = threadInfo.Fname,
            Tid = long.TryParse(threadInfo.Tid, out var tid) ? tid : 0,
            Pid = threadInfo.Pid,
            VoteInfo = threadInfo.PollInfo != null ? VoteInfoMapper.FromTbData(threadInfo.PollInfo) : null
        };
    }
}
