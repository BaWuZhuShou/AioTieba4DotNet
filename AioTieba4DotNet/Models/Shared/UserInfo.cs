using AioTieba4DotNet.Enums;

namespace AioTieba4DotNet.Models.Shared;

/// <summary>
///     用户信息
/// </summary>
public class UserInfo
{
    /// <summary>
    ///     user_id
    /// </summary>
    public long UserId { get; init; }

    /// <summary>
    ///     portrait
    /// </summary>
    public string Portrait { get; init; } = "";

    /// <summary>
    ///     用户名
    /// </summary>
    public string UserName { get; init; } = "";

    /// <summary>
    ///     旧版昵称
    /// </summary>
    public string NickNameOld { get; init; } = "";

    /// <summary>
    ///     新版昵称
    /// </summary>
    public string NickNameNew { get; init; } = "";

    /// <summary>
    ///     用户个人主页uid
    /// </summary>
    public long TiebaUid { get; init; }

    /// <summary>
    ///     贴吧成长等级
    /// </summary>
    public int GLevel { get; init; }

    /// <summary>
    ///     性别
    /// </summary>
    public Gender Gender { get; init; } = Gender.Unknown;

    /// <summary>
    ///     吧龄 以年为单位
    /// </summary>
    public float Age { get; init; }

    /// <summary>
    ///     发帖数
    /// </summary>
    public int PostNum { get; init; }

    /// <summary>
    ///     获赞数
    /// </summary>
    public int AgreeNum { get; init; }

    /// <summary>
    ///     粉丝数
    /// </summary>
    public int FanNum { get; init; }

    /// <summary>
    ///     关注数
    /// </summary>
    public int FollowNum { get; init; }

    /// <summary>
    ///     关注贴吧数
    /// </summary>
    public int ForumNum { get; init; }

    /// <summary>
    ///     个性签名
    /// </summary>
    public string Sign { get; init; } = "";

    /// <summary>
    ///     ip归属地
    /// </summary>
    public string Ip { get; init; } = "";

    /// <summary>
    ///     印记信息
    /// </summary>
    public List<string> Icons { get; init; } = [];

    /// <summary>
    ///     是否超级会员
    /// </summary>
    public bool IsVip { get; init; }

    /// <summary>
    ///     是否大神
    /// </summary>
    public bool IsGod { get; init; }

    /// <summary>
    ///     是否被永久封禁屏蔽
    /// </summary>
    public bool IsBlocked { get; init; }

    /// <summary>
    ///     关注吧列表的公开状态
    /// </summary>
    public PrivLike PrivLike { get; init; } = PrivLike.Public;

    /// <summary>
    ///     帖子评论权限
    /// </summary>
    public PrivReply PrivReply { get; init; } = PrivReply.All;

    /// <summary>
    ///     UK
    /// </summary>
    public long Uk { get; init; }

    /// <summary>
    ///     百度 UK
    /// </summary>
    public string BdUk { get; init; } = "";

    /// <summary>
    ///     用户昵称
    /// </summary>
    public string NickName => !string.IsNullOrEmpty(NickNameNew) ? NickNameNew : NickNameOld;

    /// <summary>
    ///     显示名称
    /// </summary>
    public string ShowName => !string.IsNullOrEmpty(NickNameNew) ? NickNameNew :
        !string.IsNullOrEmpty(NickNameOld) ? NickNameOld : UserName;

    /// <summary>
    ///     用于在日志中记录用户信息
    /// </summary>
    public string LogName => !string.IsNullOrEmpty(UserName) ? UserName :
        !string.IsNullOrEmpty(Portrait) ? $"{NickName}/{Portrait}" : UserId.ToString();

    /// <summary>
    ///     基于 UserId 的比较器
    /// </summary>
    public static IEqualityComparer<UserInfo> UserIdComparer { get; } = new UserIdEqualityComparer();

    /// <summary>
    ///     转换为字符串
    /// </summary>
    /// <returns>用户名、头像 ID 或 UserId</returns>
    public override string ToString()
    {
        return !string.IsNullOrEmpty(UserName) ? UserName :
            !string.IsNullOrEmpty(Portrait) ? Portrait : UserId.ToString();
    }

    /// <summary>
    ///     判断是否相等
    /// </summary>
    /// <param name="obj">比较对象</param>
    /// <returns>True 如果 UserId 相等</returns>
    public override bool Equals(object? obj)
    {
        return obj is UserInfo user && UserId == user.UserId;
    }

    /// <summary>
    ///     获取哈希值
    /// </summary>
    /// <returns>UserId 的哈希值</returns>
    public override int GetHashCode()
    {
        return UserId.GetHashCode();
    }

    private sealed class UserIdEqualityComparer : IEqualityComparer<UserInfo>
    {
        public bool Equals(UserInfo? x, UserInfo? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null) return false;
            if (y is null) return false;
            return x.UserId == y.UserId;
        }

        public int GetHashCode(UserInfo obj)
        {
            return obj.UserId.GetHashCode();
        }
    }
}
