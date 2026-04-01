using AioTieba4DotNet.Models.Threads;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class ForumTMapper
{
    internal static ForumT FromTbData(FrsPageResIdl.Types.DataRes dataRes)

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


    internal static ForumT FromTbData(SimpleForum? forumInfo)

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
}
