namespace AioTieba4DotNet.Models.Forums;

/// <summary>
///     表示 aiotieba `get_self_follow_forums_v1` 中的 <c>SelfFollowForumV1</c> 项信息。
/// </summary>
public class SelfFollowForumV1
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
}
