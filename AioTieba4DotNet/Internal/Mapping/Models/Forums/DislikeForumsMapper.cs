using AioTieba4DotNet.Models.Forums;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class DislikeForumsMapper
{
    internal static DislikeForums FromTbData(GetDislikeListResIdl.Types.DataRes data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var objs = data.ForumList.Select(static forum => new DislikeForum
        {
            Fid = (ulong)forum.ForumId,
            Fname = forum.ForumName,
            MemberNum = forum.MemberCount,
            PostNum = forum.PostNum,
            ThreadNum = forum.ThreadNum,
            IsFollowed = false
        }).ToList();

        var page = new DislikeForumsPage
        {
            CurrentPage = data.CurPage, HasMore = data.HasMore != 0, HasPrevious = data.CurPage > 1
        };

        return new DislikeForums(objs, page);
    }
}
