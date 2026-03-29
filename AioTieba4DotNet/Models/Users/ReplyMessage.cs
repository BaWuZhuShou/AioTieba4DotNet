using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Users;

/// <summary>
///     回复消息
/// </summary>
public class ReplyMessage
{
    /// <summary>
    ///     消息内容
    /// </summary>
    public string Content { get; init; } = "";

    /// <summary>
    ///     吧名
    /// </summary>
    public string Fname { get; init; } = "";

    /// <summary>
    ///     主题帖 ID
    /// </summary>
    public long ThreadId { get; init; }

    /// <summary>
    ///     被引用回复 ID
    /// </summary>
    public long QuotePostId { get; init; }

    /// <summary>
    ///     当前回复 ID
    /// </summary>
    public long PostId { get; init; }

    /// <summary>
    ///     回复人
    /// </summary>
    public UserInfo? Replyer { get; init; }

    /// <summary>
    ///     被引用用户
    /// </summary>
    public UserInfo? QuoteUser { get; init; }

    /// <summary>
    ///     主题帖作者
    /// </summary>
    public UserInfo? ThreadAuthorUser { get; init; }

    /// <summary>
    ///     是否为楼中楼消息
    /// </summary>
    public bool IsFloor { get; init; }

    /// <summary>
    ///     消息时间戳
    /// </summary>
    public long Time { get; init; }
}
