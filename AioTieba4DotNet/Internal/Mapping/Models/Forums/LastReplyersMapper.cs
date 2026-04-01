using AioTieba4DotNet.Models.Forums;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class LastReplyersMapper
{
    internal static LastReplyers FromTbData(FrsPageResIdl4lp.Types.DataRes data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var forum = new Forum
        {
            Fid = data.Forum?.Id ?? 0,
            Fname = data.Forum?.Name ?? string.Empty
        };

        var pageSize = data.Page?.PageSize ?? 0;
        var currentPage = data.Page?.CurrentPage ?? 0;
        if (currentPage == 0 && pageSize != 0)
            currentPage = 1;

        var page = new LastReplyersPage
        {
            PageSize = pageSize,
            CurrentPage = currentPage,
            TotalPage = data.Page?.TotalPage ?? 0,
            TotalCount = data.Page?.TotalCount ?? 0,
            HasMore = data.Page?.HasMore != 0,
            HasPrevious = data.Page?.HasPrev != 0
        };

        var threads = data.ThreadList.Select(thread =>
        {
            var author = thread.Author ?? new User();
            var lastReplyer = thread.LastReplyer ?? new User();
            return new LastReplyerThread
            {
                Title = thread.Title,
                Fid = (ulong)forum.Fid,
                Fname = forum.Fname,
                Tid = thread.Id,
                Pid = thread.FirstPostId,
                User = new LastReplyerUser
                {
                    UserId = author.Id,
                    Portrait = NormalizePortrait(author.Portrait),
                    UserName = author.Name,
                    NickNameOld = author.NameShow
                },
                LastReplyer = new LastReplyer
                {
                    UserId = lastReplyer.Id,
                    UserName = lastReplyer.Name,
                    NickNameOld = lastReplyer.NameShow
                },
                IsGood = thread.IsGood != 0,
                IsTop = thread.IsTop != 0,
                CreateTime = thread.CreateTime,
                LastTime = thread.LastTimeInt
            };
        }).ToList();

        return new LastReplyers(threads, page, forum);
    }

    private static string NormalizePortrait(string portrait)
    {
        if (portrait.Contains('?', StringComparison.Ordinal) && portrait.Length >= 13)
            return portrait[..^13];

        return portrait;
    }
}
