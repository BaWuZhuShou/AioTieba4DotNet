using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class VoteInfoMapper
{
    internal static VoteInfo? FromTbData(PollInfo? voteInfo)

    {
        if (voteInfo == null || string.IsNullOrEmpty(voteInfo.Title)) return null;


        return new VoteInfo
        {
            Title = voteInfo.Title,
            IsMulti = voteInfo.IsMulti == 1,
            Options = voteInfo.Options.Select(VoteOptionMapper.FromTbData).ToList(),
            TotalVotes = voteInfo.TotalPoll,
            TotalUsers = voteInfo.TotalNum
        };
    }
}
