using System.Diagnostics.CodeAnalysis;
using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Users;

/// <summary>
///     用户信息 (面板接口)
/// </summary>
[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty",
    Justification = "This payload-specific subtype is intentionally empty so callers can distinguish user info returned by panel endpoints without changing the shared base model.")]
public class UserInfoPanel : UserInfo
{
}
