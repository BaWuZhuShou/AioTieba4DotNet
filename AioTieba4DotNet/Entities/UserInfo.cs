using AioTieba4DotNet.Enums;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Entities;

public class UserInfo
{
    /// <summary>
    /// user_id
    /// </summary>
    public long UserId { get; init; }

    /// <summary>
    /// portrait
    /// </summary>
    public string Portrait { get; init; } = "";

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; init; } = "";

    /// <summary>
    /// 旧版昵称
    /// </summary>
    public string NickNameOld { get; init; } = "";

    /// <summary>
    /// 新版昵称
    /// </summary>
    public string NickNameNew { get; init; } = "";

    /// <summary>
    /// 用户个人主页uid
    /// </summary>
    public long TiebaUid { get; init; }

    /// <summary>
    /// 贴吧成长等级
    /// </summary>
    public int GLevel { get; init; }

    /// <summary>
    /// 性别
    /// </summary>
    public Gender Gender { get; init; } = Gender.Unknown;

    /// <summary>
    /// 吧龄 以年为单位
    /// </summary>
    public float Age { get; init; }

    /// <summary>
    /// 发帖数
    /// </summary>
    public int PostNum { get; init; }

    /// <summary>
    /// 获赞数
    /// </summary>
    public int AgreeNum { get; init; }

    /// <summary>
    /// 粉丝数
    /// </summary>
    public int FanNum { get; init; }

    /// <summary>
    /// 关注数
    /// </summary>
    public int FollowNum { get; init; }

    /// <summary>
    /// 关注贴吧数
    /// </summary>
    public int ForumNum { get; init; }

    /// <summary>
    /// 个性签名
    /// </summary>
    public string Sign { get; init; } = "";

    /// <summary>
    /// ip归属地
    /// </summary>
    public string Ip { get; init; } = "";

    /// <summary>
    /// 印记信息
    /// </summary>
    public List<string> Icons { get; init; } = [];

    /// <summary>
    /// 是否超级会员
    /// </summary>
    public bool IsVip { get; init; }

    /// <summary>
    /// 是否大神
    /// </summary>
    public bool IsGod { get; init; }

    /// <summary>
    /// 是否被永久封禁屏蔽
    /// </summary>
    public bool IsBlocked { get; init; }

    /// <summary>
    /// 关注吧列表的公开状态
    /// </summary>
    public PrivLike PrivLike { get; init; } = PrivLike.Public;

    /// <summary>
    /// 帖子评论权限
    /// </summary>
    public PrivReply PrivReply { get; init; } = PrivReply.All;

    public long Uk { get; init; }
    public string BdUk { get; init; } = "";

    /// <summary>
    /// 用户昵称
    /// </summary>
    public string NickName => !string.IsNullOrEmpty(NickNameNew) ? NickNameNew : NickNameOld;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string ShowName => !string.IsNullOrEmpty(NickNameNew) ? NickNameNew :
        !string.IsNullOrEmpty(NickNameOld) ? NickNameOld : UserName;

    /// <summary>
    /// 用于在日志中记录用户信息
    /// </summary>
    public string LogName => !string.IsNullOrEmpty(UserName) ? UserName :
        !string.IsNullOrEmpty(Portrait) ? $"{NickName}/{Portrait}" : UserId.ToString();

    public override string ToString()
    {
        return !string.IsNullOrEmpty(UserName) ? UserName :
            !string.IsNullOrEmpty(Portrait) ? Portrait : UserId.ToString();
    }

    public override bool Equals(object? obj)
    {
        return obj is UserInfo user && UserId == user.UserId;
    }

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

    public static IEqualityComparer<UserInfo> UserIdComparer { get; } = new UserIdEqualityComparer();

    /// <summary>
    /// 从贴吧原始数据转换
    /// </summary>
    /// <param name="dataRes"></param>
    /// <returns>UserInfo?</returns>
    public static UserInfo? FromTbData(PostInfoList? dataRes)
    {
        if (dataRes == null) return null;

        var userId = dataRes.UserId;
        var portrait = dataRes.UserPortrait ?? "";
        if (portrait.Contains('?')) portrait = portrait[..^13];

        var userName = dataRes.UserName;
        var nickNameNew = dataRes.NameShow;

        return new UserInfo { UserId = userId, Portrait = portrait, UserName = userName, NickNameNew = nickNameNew };
    }

    /// <summary>
    /// 从 JSON 数据转换
    /// </summary>
    /// <param name="data"></param>
    /// <returns>UserInfo</returns>
    public static UserInfo FromTbData(JObject data)
    {
        var portrait = data.GetValue("portrait")?.Value<string>() ?? "";
        if (portrait.Contains('?')) portrait = portrait[..^13];

        return new UserInfo
        {
            UserId = data.GetValue("id")?.Value<long>() ?? 0,
            Portrait = portrait,
            UserName = data.GetValue("name")?.Value<string>() ?? "",
            NickNameNew = data.GetValue("name_show")?.Value<string>() ?? ""
        };
    }
}
