namespace AioTieba4DotNet.Api.GetForum.Entities;

public class Forum
{
    public int Fid { get; set; } = 0;
    public string Fname { get; set; } = "";
    public string Category { get; set; } = "";
    public string Subcategory { get; set; } = "";
    public string SmallAvatar { get; set; } = "";
    public string Slogan { get; set; } = "";
    public int MemberNum { get; set; } = 0;
    public int PostNum { get; set; } = 0;
    public int ThreadNum { get; set; } = 0;

    public bool HasBaWu { get; set; }= false;
    // 贴吧信息

    public static Forum FromTbData(IDictionary<string, object> dataMap)
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

        return new Forum()
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

    public override string ToString()
    {
        return
            $"{nameof(Fid)}: {Fid}, {nameof(Fname)}: {Fname}, {nameof(Category)}: {Category}, {nameof(Subcategory)}: {Subcategory}, {nameof(SmallAvatar)}: {SmallAvatar}, {nameof(Slogan)}: {Slogan}, {nameof(MemberNum)}: {MemberNum}, {nameof(PostNum)}: {PostNum}, {nameof(ThreadNum)}: {ThreadNum}, {nameof(HasBaWu)}: {HasBaWu}";
    }
}
