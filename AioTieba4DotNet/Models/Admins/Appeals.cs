using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Admins;

/// <summary>
///     解封申诉项
/// </summary>
public sealed class Appeal
{
    /// <summary>
    ///     用户 ID
    /// </summary>
    public long UserId { get; init; }

    /// <summary>
    ///     portrait
    /// </summary>
    public string Portrait { get; init; } = string.Empty;

    /// <summary>
    ///     用户名
    /// </summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>
    ///     昵称
    /// </summary>
    public string NickName { get; init; } = string.Empty;

    /// <summary>
    ///     申诉 ID
    /// </summary>
    public long AppealId { get; init; }

    /// <summary>
    ///     申诉理由
    /// </summary>
    public string AppealReason { get; init; } = string.Empty;

    /// <summary>
    ///     申诉时间戳（秒）
    /// </summary>
    public long AppealTime { get; init; }

    /// <summary>
    ///     封禁理由
    /// </summary>
    public string PunishReason { get; init; } = string.Empty;

    /// <summary>
    ///     封禁开始时间戳（秒）
    /// </summary>
    public long PunishTime { get; init; }

    /// <summary>
    ///     封禁天数
    /// </summary>
    public int PunishDay { get; init; }

    /// <summary>
    ///     操作人用户名
    /// </summary>
    public string OperatorName { get; init; } = string.Empty;

    /// <summary>
    ///     显示名称
    /// </summary>
    public string ShowName => !string.IsNullOrEmpty(NickName) ? NickName : UserName;
}

/// <summary>
///     解封申诉列表
/// </summary>
public sealed class Appeals(List<Appeal> objs, bool hasMore) : Containers<Appeal>(objs)
{
    /// <summary>
    ///     是否还有下一页
    /// </summary>
    public bool HasMore { get; } = hasMore;
}
