namespace AioTieba4DotNet.Api.Entities;

public class VoteOption
{
    public long VoteNum { get; set; } = 0;
    public string Text { get; set; } = string.Empty;

    public static VoteOption FromTbData(PollInfo.Types.PollOption pollOption)
    {
        return new VoteOption()
        {
            VoteNum = pollOption.Num,
            Text = pollOption.Text
        };
    }
}
