namespace AioTieba4DotNet.Api.GetThreads.Entities;

/// <summary>
///     吧信息
/// </summary>
public class ForumT
{
    /// <summary>
    ///     贴吧id
    /// </summary>
    public long Fid { get; init; }

    /// <summary>
    ///     贴吧名
    /// </summary>
    public string Fname { get; init; } = "";

    /// <summary>
    ///     一级分类
    /// </summary>
    public string Category { get; init; } = "";

    /// <summary>
    ///     二级分类
    /// </summary>
    public string Subcategory { get; init; } = "";

    /// <summary>
    ///     吧会员数
    /// </summary>
    public int MemberNum { get; init; }

    /// <summary>
    ///     发帖量
    /// </summary>
    public int PostNum { get; init; }

    /// <summary>
    ///     主题帖数
    /// </summary>
    public int ThreadNum { get; init; }

    /// <summary>
    ///     是否有吧务
    /// </summary>
    public bool HasBaWu { get; init; }

    /// <summary>
    ///     是否有吧规
    /// </summary>
    public bool HasRule { get; init; }

    /// <summary>
    ///     从贴吧原始数据转换
    /// </summary>
    /// <param name="dataRes">Protobuf 吧响应数据</param>
    /// <returns>吧信息实体</returns>
    public static ForumT FromTbData(FrsPageResIdl.Types.DataRes dataRes)
    {
        var forumInfo = dataRes.Forum;
        return new ForumT
        {
            Fid = forumInfo.Id,
            Fname = forumInfo.Name,
            Category = forumInfo.FirstClass,
            Subcategory = forumInfo.SecondClass,
            MemberNum = forumInfo.MemberNum,
            PostNum = forumInfo.PostNum,
            ThreadNum = forumInfo.ThreadNum,
            HasBaWu = forumInfo.Managers.Count != 0,
            HasRule = dataRes.ForumRule.HasForumRule == 1
        };
    }

    /// <summary>
    ///     从贴吧原始数据转换
    /// </summary>
    /// <param name="forumInfo">Protobuf 简版吧信息数据</param>
    /// <returns>吧信息实体</returns>
    public static ForumT FromTbData(SimpleForum? forumInfo)
    {
        if (forumInfo == null) return new ForumT();

        return new ForumT
        {
            Fid = forumInfo.Id,
            Fname = forumInfo.Name,
            MemberNum = forumInfo.MemberNum,
            PostNum = forumInfo.PostNum,
            Category = forumInfo.FirstClass,
            Subcategory = forumInfo.SecondClass
        };
    }

    /// <summary>
    ///     格式设置
    /// </summary>
    /// <returns>string</returns>
    public override string ToString()
    {
        return
            $"{nameof(Fid)}: {Fid}, {nameof(Fname)}: {Fname}, {nameof(Category)}: {Category}, {nameof(Subcategory)}: {Subcategory}, {nameof(MemberNum)}: {MemberNum}, {nameof(PostNum)}: {PostNum}, {nameof(ThreadNum)}: {ThreadNum}, {nameof(HasBaWu)}: {HasBaWu}, {nameof(HasRule)}: {HasRule}";
    }
}
