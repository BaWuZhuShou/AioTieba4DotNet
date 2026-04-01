namespace AioTieba4DotNet.Models.Forums;

/// <summary>
///     当前账号关注的贴吧信息
/// </summary>
public class SelfFollowForum
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
    ///     当前账号是否已在该吧签到
    /// </summary>
    public bool IsSigned { get; set; }
}
