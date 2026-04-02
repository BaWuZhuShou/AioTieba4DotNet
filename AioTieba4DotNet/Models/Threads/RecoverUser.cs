using System;

namespace AioTieba4DotNet.Models.Threads;

/// <summary>
///     表示回收站条目的作者信息。
/// </summary>
public sealed class RecoverUser
{
    /// <summary>
    ///     获取用户名。
    /// </summary>
    /// <value>A user name.</value>
    public string UserName { get; init; } = string.Empty;

    /// <summary>
    ///     获取 portrait。
    /// </summary>
    /// <value>A portrait identifier.</value>
    public string Portrait { get; init; } = string.Empty;

    /// <summary>
    ///     获取新版昵称。
    /// </summary>
    /// <value>A new-style nickname.</value>
    public string NickNameNew { get; init; } = string.Empty;

    /// <summary>
    ///     获取用户昵称。
    /// </summary>
    /// <value>A nickname.</value>
    public string NickName => NickNameNew;

    /// <summary>
    ///     获取显示名称。
    /// </summary>
    /// <value>A display name.</value>
    public string ShowName => !string.IsNullOrEmpty(NickNameNew) ? NickNameNew : UserName;

    /// <summary>
    ///     获取用于日志的名称。
    /// </summary>
    /// <value>A log-friendly name.</value>
    public string LogName
    {
        get
        {
            if (!string.IsNullOrEmpty(UserName)) return UserName;
            return !string.IsNullOrEmpty(Portrait) ? $"{NickNameNew}/{Portrait}" : string.Empty;
        }
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return !string.IsNullOrEmpty(UserName) ? UserName : Portrait;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is RecoverUser other &&
               string.Equals(Portrait, other.Portrait, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Portrait);
    }
}
