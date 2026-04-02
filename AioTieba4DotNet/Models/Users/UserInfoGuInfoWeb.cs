using System.Diagnostics.CodeAnalysis;
using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Users;

/// <summary>
///     表示 aiotieba `get_uinfo_getuserinfo_web` 中的 <c>UserInfo_guinfo_web</c> 用户信息。
/// </summary>
[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty",
    Justification = "This payload-specific subtype is intentionally empty so callers can distinguish user info returned by the web guinfo source without changing the shared base model.")]
public class UserInfoGuInfoWeb : UserInfo
{
}
