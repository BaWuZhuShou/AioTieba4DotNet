namespace AioTieba4DotNet.Models.Forums;

/// <summary>
///     首页推荐屏蔽的贴吧信息
/// </summary>
public class DislikeForum
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
    ///     会员数
    /// </summary>
    public int MemberNum { get; set; }

    /// <summary>
    ///     发帖数
    /// </summary>
    public long PostNum { get; set; }

    /// <summary>
    ///     主题帖数
    /// </summary>
    public long ThreadNum { get; set; }

    /// <summary>
    ///     当前账号是否已关注该吧
    /// </summary>
    public bool IsFollowed { get; set; }
}
