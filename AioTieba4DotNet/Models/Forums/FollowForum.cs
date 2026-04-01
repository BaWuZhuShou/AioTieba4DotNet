namespace AioTieba4DotNet.Models.Forums;

/// <summary>
///     用户关注的贴吧信息
/// </summary>
public class FollowForum
{
    /// <summary>
    ///     吧 ID
    /// </summary>
    public ulong Fid { get; set; }

    /// <summary>
    ///     吧名
    /// </summary>
    public string Fname { get; set; } = string.Empty;

    /// <summary>
    ///     当前账号在该吧的等级
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    ///     当前账号在该吧的经验值
    /// </summary>
    public int Exp { get; set; }
}
