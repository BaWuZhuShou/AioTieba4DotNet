namespace AioTieba4DotNet.Models.Users;

/// <summary>
///     表示用户在指定贴吧内的信息。
/// </summary>
public class UserForumInfo
{
    /// <summary>
    ///     Gets the user snapshot.
    /// </summary>
    /// <value>A user snapshot.</value>
    public UserInfoUf User { get; init; } = new();

    /// <summary>
    ///     Gets the forum name.
    /// </summary>
    /// <value>A forum name.</value>
    public string Fname { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the small forum avatar URL.
    /// </summary>
    /// <value>A small forum avatar URL.</value>
    public string SmallAvatar { get; init; } = string.Empty;

    /// <summary>
    ///     Gets a value that indicates whether the current account follows the forum.
    /// </summary>
    /// <value><see langword="true" /> if the forum is followed; otherwise, <see langword="false" />.</value>
    public bool IsFollow { get; init; }

    /// <summary>
    ///     Gets the follow duration in days.
    /// </summary>
    /// <value>A follow duration in days.</value>
    public int FollowDays { get; init; }

    /// <summary>
    ///     Gets the sign-in day count.
    /// </summary>
    /// <value>A sign-in day count.</value>
    public int SignDays { get; init; }

    /// <summary>
    ///     Gets the thread count in the forum.
    /// </summary>
    /// <value>A thread count.</value>
    public int ThreadNum { get; init; }

    /// <summary>
    ///     Gets the current-day post count in the forum.
    /// </summary>
    /// <value>A current-day post count.</value>
    public int DayPostNum { get; init; }

    /// <summary>
    ///     Gets the member rank within the forum.
    /// </summary>
    /// <value>A member rank.</value>
    public int MemberRank { get; init; }

    /// <summary>
    ///     Gets the current-day sign rank within the forum.
    /// </summary>
    /// <value>A current-day sign rank.</value>
    public int DaySignRank { get; init; }

    /// <summary>
    ///     Gets the forum level.
    /// </summary>
    /// <value>A forum level.</value>
    public int Level { get; init; }

    /// <summary>
    ///     Gets the forum level title.
    /// </summary>
    /// <value>A forum level title.</value>
    public string LevelName { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the current experience value.
    /// </summary>
    /// <value>An experience value.</value>
    public int Exp { get; init; }

    /// <summary>
    ///     Gets the experience required for the next level.
    /// </summary>
    /// <value>An experience target for level-up.</value>
    public int LevelupExp { get; init; }

    /// <summary>
    ///     Gets the forum role name.
    /// </summary>
    /// <value>A forum role name.</value>
    public string RoleName { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the identity marker.
    /// </summary>
    /// <value>An identity marker.</value>
    public string Identify { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the highlighted consecutive sign-in day count.
    /// </summary>
    /// <value>A highlighted consecutive sign-in day count.</value>
    public int HighLightSignDays { get; init; }
}
