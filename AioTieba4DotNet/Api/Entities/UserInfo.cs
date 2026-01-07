using AioTieba4DotNet.Api.Login.Entities;

namespace AioTieba4DotNet.Api.Entities;

/// <summary>
/// 表示有关用户的信息，包括其 ID、用户名、肖像和昵称。
/// </summary>
public class UserInfo : UserInfoLogin
{
    /// <summary>
    /// 新版昵称
    /// </summary>
    public string NickNameNew { get; init; } = "";

    public string NickName => NickNameNew;
    public string ShowName => NickNameNew == "" ? UserName : NickNameNew;

    public string LogName =>
        UserName != "" ? UserName : Portrait != "" ? $"{NickNameNew}/{Portrait}" : UserId.ToString();

    public override string ToString()
    {
        return
            $"{nameof(UserId)}: {UserId}, {nameof(Portrait)}: {Portrait}, {nameof(UserName)}: {UserName}, {nameof(NickNameNew)}: {NickNameNew}";
    }

    protected bool Equals(UserInfo other)
    {
        return UserId == other.UserId;
    }

    private sealed class UserIdEqualityComparer : IEqualityComparer<UserInfo>
    {
        public bool Equals(UserInfo? x, UserInfo? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null) return false;
            if (y is null) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.UserId == y.UserId;
        }

        public int GetHashCode(UserInfo obj)
        {
            return obj.UserId.GetHashCode();
        }
    }

    public static IEqualityComparer<UserInfo> UserIdComparer { get; } = new UserIdEqualityComparer();

    public static UserInfo FromTbData(PostInfoList dataRes)
    {
        var userId = dataRes.UserId;
        var portrait = dataRes.UserPortrait;
        if (portrait.Contains('?'))
        {
            portrait = portrait[..^13];
        }

        var userName = dataRes.UserName;
        var nickNameNew = dataRes.NameShow;

        return new UserInfo()
        {
            UserId = userId,
            Portrait = portrait,
            UserName = userName,
            NickNameNew = nickNameNew,
        };
    }
}