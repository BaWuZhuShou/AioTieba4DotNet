using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Threads;

/// <summary>
///     用户信息
/// </summary>
public class UserInfoT : UserInfo
{
    /// <summary>
    ///     等级
    /// </summary>
    public int Level { get; init; }

    /// <summary>
    ///     是否吧务
    /// </summary>
    public bool IsBawu { get; init; }
}
