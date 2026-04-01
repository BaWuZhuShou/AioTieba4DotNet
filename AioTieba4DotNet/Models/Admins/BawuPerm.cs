namespace AioTieba4DotNet.Models.Admins;

/// <summary>
///     吧务权限信息
/// </summary>
public sealed class BawuPerm
{
    /// <summary>
    ///     已分配权限集合
    /// </summary>
    public BawuPermType Permissions { get; init; }
}
