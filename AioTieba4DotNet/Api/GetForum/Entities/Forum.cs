namespace AioTieba4DotNet.Api.GetForum.Entities;

/// <summary>
///     吧信息 (JSON 接口)
/// </summary>
public class Forum
{
    /// <summary>
    ///     吧 ID
    /// </summary>
    public int Fid { get; set; }

    /// <summary>
    ///     吧名
    /// </summary>
    public string Fname { get; set; } = "";

    /// <summary>
    ///     一级分类
    /// </summary>
    public string Category { get; set; } = "";

    /// <summary>
    ///     二级分类
    /// </summary>
    public string Subcategory { get; set; } = "";

    /// <summary>
    ///     小头像
    /// </summary>
    public string SmallAvatar { get; set; } = "";

    /// <summary>
    ///     吧标语
    /// </summary>
    public string Slogan { get; set; } = "";

    /// <summary>
    ///     会员数
    /// </summary>
    public int MemberNum { get; set; }

    /// <summary>
    ///     发帖数
    /// </summary>
    public int PostNum { get; set; }

    /// <summary>
    ///     主题帖数
    /// </summary>
    public int ThreadNum { get; set; }

    /// <summary>
    ///     是否有吧务
    /// </summary>
    public bool HasBaWu { get; set; }

    /// <summary>
    ///     从贴吧原始数据转换
    /// </summary>
    /// <param name="dataMap">JSON 响应数据字典</param>
    /// <returns>吧信息实体</returns>
    internal static Forum FromTbData(IDictionary<string, object> dataMap)
    {
        var fid = Convert.ToInt32(dataMap["id"]);
        var fname = dataMap["name"] as string ?? "";
        var category = dataMap["first_class"] as string ?? "";
        var subcategory = dataMap["second_class"] as string ?? "";
        var smallAvatar = dataMap["avatar"] as string ?? "";
        var slogan = dataMap["slogan"] as string ?? "";
        var memberNum = Convert.ToInt32(dataMap["member_num"]);
        var postNum = Convert.ToInt32(dataMap["post_num"]);
        var threadNum = Convert.ToInt32(dataMap["thread_num"]);
        var hasBaWu = dataMap.ContainsKey("managers");

        return new Forum
        {
            Fid = fid,
            Fname = fname,
            Category = category,
            Subcategory = subcategory,
            SmallAvatar = smallAvatar,
            Slogan = slogan,
            MemberNum = memberNum,
            PostNum = postNum,
            ThreadNum = threadNum,
            HasBaWu = hasBaWu
        };
    }

    /// <summary>
    ///     转换为字符串
    /// </summary>
    /// <returns>吧信息摘要</returns>
    public override string ToString()
    {
        return
            $"{nameof(Fid)}: {Fid}, {nameof(Fname)}: {Fname}, {nameof(Category)}: {Category}, {nameof(Subcategory)}: {Subcategory}, {nameof(SmallAvatar)}: {SmallAvatar}, {nameof(Slogan)}: {Slogan}, {nameof(MemberNum)}: {MemberNum}, {nameof(PostNum)}: {PostNum}, {nameof(ThreadNum)}: {ThreadNum}, {nameof(HasBaWu)}: {HasBaWu}";
    }
}
