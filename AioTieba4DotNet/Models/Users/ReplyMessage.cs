using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Users;

public class ReplyMessage
{
    public string Content { get; init; } = "";

    public string Fname { get; init; } = "";

    public long ThreadId { get; init; }

    public long QuotePostId { get; init; }

    public long PostId { get; init; }

    public UserInfo? Replyer { get; init; }

    public UserInfo? QuoteUser { get; init; }

    public UserInfo? ThreadAuthorUser { get; init; }

    public bool IsFloor { get; init; }

    public long Time { get; init; }
}
