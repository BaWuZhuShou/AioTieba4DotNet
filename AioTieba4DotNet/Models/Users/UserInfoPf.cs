using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Users;

/// <summary>
///     用户信息 (个人主页接口)
/// </summary>
public class UserInfoPf : UserInfo
{
    /// <summary>
    ///     虚拟形象
    /// </summary>
    public required VirtualImagePf VImage { get; init; }
}
