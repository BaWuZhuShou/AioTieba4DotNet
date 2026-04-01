using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Users;

public sealed class LoginResult
{
    public required UserInfo User { get; init; }

    public string Tbs { get; init; } = string.Empty;
}
