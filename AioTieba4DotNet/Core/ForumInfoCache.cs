using Microsoft.Extensions.Caching.Memory;

namespace AioTieba4DotNet.Core;

public class ForumInfoCache
{
    private readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    public ulong GetForumId(string forumName)
    {
        return _cache.TryGetValue(forumName, out ulong result) ? result : 0;
    }

    public string? GetForumName(ulong forumId)
    {
        return _cache.TryGetValue(forumId.ToString(), out string? result) ? result : string.Empty;
    }

    public void SetForumName(ulong forumId, string forumName)
    {
        _cache.Set(forumId.ToString(), forumName);
        _cache.Set(forumName, forumId);
    }
}