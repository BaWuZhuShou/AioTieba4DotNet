using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Users;

public class AtMessage
{
    public string Content { get; init; } = "";

    public string Fname { get; init; } = "";

    public long ThreadId { get; init; }

    public long PostId { get; init; }

    public UserInfo? Replyer { get; init; }

    public bool IsFloor { get; init; }

    public bool IsFirstPost { get; init; }

    public long Time { get; init; }
}
