using AioTieba4DotNet.Api.Entities;
using AioTieba4DotNet.Enums;

namespace AioTieba4DotNet.Api.GetThreads.Entities;

/// <summary>
/// 用户信息
/// </summary>
public class UserInfoT : UserInfo
{
    /// <summary>
    /// 等级
    /// </summary>
    public int Level { get; init; }

    /// <summary>
    /// 贴吧成长等级
    /// </summary>
    public uint GLevel { get; init; }

    /// <summary>
    /// 性别
    /// </summary>
    public Gender Gender { get; init; }

    /// <summary>
    /// 印记信息
    /// </summary>
    public string[] Icons { get; init; } = [];

    /// <summary>
    /// 是否吧务
    /// </summary>
    public bool IsBawu { get; init; }

    /// <summary>
    /// 是否超级会员
    /// </summary>
    public bool IsVip { get; init; }

    /// <summary>
    /// 是否大神
    /// </summary>
    public bool IsGod { get; init; }

    /// <summary>
    /// 关注吧列表的公开状态
    /// </summary>
    public PrivLike PrivLike { get; init; }

    /// <summary>
    /// 帖子评论权限
    /// </summary>
    public PrivReply PrivReply { get; init; }

    /// <summary>
    /// 从贴吧原始数据转换
    /// </summary>
    /// <param name="dataProto"></param>
    /// <returns>UserInfoT</returns>
    public static UserInfoT FromTbData(User dataProto)
    {
        var portrait = dataProto.Portrait;
        if (portrait.Contains('?'))
        {
            portrait = portrait[..^13];
        }

        var dataProtoId = dataProto.Id;
        var userName = dataProto.Name;
        var nickNameNew = dataProto.NameShow;
        var level = dataProto.LevelId;
        var gLevel = dataProto.UserGrowth.LevelId;
        var gender = (Gender)dataProto.Gender;
        var icons = dataProto.Iconinfo.Where(i => !string.IsNullOrEmpty(i.Name)).Select(i => i.Name).ToArray();
        var isBawu = dataProto.IsBawu == 1;
        var isVip = dataProto.NewTshowIcon.Count != 0;
        var isGod = dataProto.NewGodData is { Status: 1 };

        return new UserInfoT
        {
            UserId = dataProtoId,
            Portrait = portrait,
            UserName = userName,
            NickNameNew = nickNameNew,
            Level = level,
            GLevel = gLevel,
            Gender = gender,
            Icons = icons,
            IsBawu = isBawu,
            IsVip = isVip,
            IsGod = isGod,
            PrivLike = dataProto.PrivSets != null && dataProto.PrivSets.Like != 0
                ? (PrivLike)dataProto.PrivSets.Like
                : PrivLike.Public,
            PrivReply = dataProto.PrivSets != null && dataProto.PrivSets.Reply != 0
                ? (PrivReply)dataProto.PrivSets.Reply
                : PrivReply.All,
        };
    }

    /// <summary>
    /// 格式设置
    /// </summary>
    /// <returns>string</returns>
    public override string ToString()
    {
        return
            $"{nameof(UserId)}: {UserId}, {nameof(UserName)}: {UserName}, {nameof(Portrait)}: {Portrait}, {nameof(NickNameNew)}: {NickNameNew}, {nameof(Level)}: {Level}, {nameof(GLevel)}: {GLevel}, {nameof(Gender)}: {Gender}, {nameof(Icons)}: {Icons}, {nameof(IsBawu)}: {IsBawu}, {nameof(IsVip)}: {IsVip}, {nameof(IsGod)}: {IsGod}, {nameof(PrivLike)}: {PrivLike}, {nameof(PrivReply)}: {PrivReply}, {nameof(NickName)}: {NickName}, {nameof(ShowName)}: {ShowName}, {nameof(LogName)}: {LogName}";
    }
}