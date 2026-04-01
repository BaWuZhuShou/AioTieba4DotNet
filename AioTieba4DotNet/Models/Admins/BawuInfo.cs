namespace AioTieba4DotNet.Models.Admins;

/// <summary>
///     吧务用户信息
/// </summary>
public sealed class BawuUser
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
    ///     新版昵称
    /// </summary>
    public string NickNameNew { get; init; } = string.Empty;

    /// <summary>
    ///     吧等级
    /// </summary>
    public int Level { get; init; }

    /// <summary>
    ///     昵称
    /// </summary>
    public string NickName => NickNameNew;

    /// <summary>
    ///     显示名称
    /// </summary>
    public string ShowName => !string.IsNullOrEmpty(NickNameNew) ? NickNameNew : UserName;

    /// <summary>
    ///     日志名称
    /// </summary>
    public string LogName => !string.IsNullOrEmpty(UserName)
        ? UserName
        : !string.IsNullOrEmpty(Portrait)
            ? $"{NickNameNew}/{Portrait}"
            : UserId.ToString();

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is BawuUser other && UserId == other.UserId;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return UserId.GetHashCode();
    }
}

/// <summary>
///     吧务团队信息
/// </summary>
public sealed class BawuInfo
{
    /// <summary>
    ///     所有吧务
    /// </summary>
    public IReadOnlyList<BawuUser> All { get; init; } = [];

    /// <summary>
    ///     吧主
    /// </summary>
    public IReadOnlyList<BawuUser> Admins { get; init; } = [];

    /// <summary>
    ///     小吧主
    /// </summary>
    public IReadOnlyList<BawuUser> Managers { get; init; } = [];

    /// <summary>
    ///     语音小编
    /// </summary>
    public IReadOnlyList<BawuUser> VoiceEditors { get; init; } = [];

    /// <summary>
    ///     图片小编
    /// </summary>
    public IReadOnlyList<BawuUser> ImageEditors { get; init; } = [];

    /// <summary>
    ///     视频小编
    /// </summary>
    public IReadOnlyList<BawuUser> VideoEditors { get; init; } = [];

    /// <summary>
    ///     广播小编
    /// </summary>
    public IReadOnlyList<BawuUser> BroadcastEditors { get; init; } = [];

    /// <summary>
    ///     吧刊主编
    /// </summary>
    public IReadOnlyList<BawuUser> JournalChiefEditors { get; init; } = [];

    /// <summary>
    ///     吧刊小编
    /// </summary>
    public IReadOnlyList<BawuUser> JournalEditors { get; init; } = [];

    /// <summary>
    ///     职业吧主
    /// </summary>
    public IReadOnlyList<BawuUser> ProfessAdmins { get; init; } = [];

    /// <summary>
    ///     第四吧主
    /// </summary>
    public IReadOnlyList<BawuUser> FourthAdmins { get; init; } = [];
}
