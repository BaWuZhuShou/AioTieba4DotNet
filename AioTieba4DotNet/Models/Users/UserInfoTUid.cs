using System.Diagnostics.CodeAnalysis;
using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Users;

/// <summary>
///     表示通过 Tieba UID 查询得到的用户信息。
/// </summary>
[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty",
    Justification = "This payload-specific subtype is intentionally empty so callers can distinguish user info returned by Tieba UID lookups without changing the shared base model.")]
public class UserInfoTUid : UserInfo
{
}
