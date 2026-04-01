using AioTieba4DotNet.Models.Forums;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class SquareForumsMapper
{
    internal static SquareForums FromTbData(GetForumSquareResIdl.Types.DataRes data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var forums = data.ForumInfo.Select(static forum => new SquareForum
        {
            Fid = forum.ForumId,
            Fname = forum.ForumName,
            MemberNum = checked((int)forum.MemberCount),
            PostNum = checked((int)forum.ThreadCount),
            IsFollowed = forum.IsLike != 0
        }).ToList();

        var page = new SquareForumsPage
        {
            PageSize = data.Page?.PageSize ?? 0,
            CurrentPage = data.Page?.CurrentPage ?? 0,
            TotalPage = data.Page?.TotalPage ?? 0,
            TotalCount = data.Page?.TotalCount ?? 0,
            HasMore = data.Page?.HasMore != 0,
            HasPrevious = data.Page?.HasPrev != 0
        };

        return new SquareForums(forums, page);
    }
}
