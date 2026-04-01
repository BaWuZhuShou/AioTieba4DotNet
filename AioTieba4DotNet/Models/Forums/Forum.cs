namespace AioTieba4DotNet.Models.Forums;

/// <summary>
///     吧信息 (JSON 接口)
/// </summary>
public class Forum
{
    /// <summary>
    ///     吧 ID
    /// </summary>
    public long Fid { get; set; }

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
    ///     转换为字符串
    /// </summary>
    /// <returns>吧信息摘要</returns>
    public override string ToString()
    {
        return
            $"{nameof(Fid)}: {Fid}, {nameof(Fname)}: {Fname}, {nameof(Category)}: {Category}, {nameof(Subcategory)}: {Subcategory}, {nameof(SmallAvatar)}: {SmallAvatar}, {nameof(Slogan)}: {Slogan}, {nameof(MemberNum)}: {MemberNum}, {nameof(PostNum)}: {PostNum}, {nameof(ThreadNum)}: {ThreadNum}, {nameof(HasBaWu)}: {HasBaWu}";
    }
}
