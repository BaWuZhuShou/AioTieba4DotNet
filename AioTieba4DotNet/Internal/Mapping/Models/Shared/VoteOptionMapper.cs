using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class VoteOptionMapper
{
    internal static VoteOption FromTbData(PollInfo.Types.PollOption pollOption)

        {

            return new VoteOption { VoteNum = pollOption.Num, Text = pollOption.Text };

        }
}
