using Microsoft.Extensions.Caching.Memory;

namespace AioTieba4DotNet.Core;

/// <summary>
///     吧信息缓存
/// </summary>
public class ForumInfoCache
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    /// <summary>
    ///     获取吧 ID
    /// </summary>
    /// <param name="forumName">吧名</param>
    /// <returns>吧 ID</returns>
    public ulong GetForumId(string forumName)
    {
        return _cache.TryGetValue(forumName, out ulong result) ? result : 0;
    }

    /// <summary>
    ///     获取吧名
    /// </summary>
    /// <param name="forumId">吧 ID</param>
    /// <returns>吧名</returns>
    public string? GetForumName(ulong forumId)
    {
        return _cache.TryGetValue(forumId.ToString(), out string? result) ? result : string.Empty;
    }

    /// <summary>
    ///     设置吧信息缓存
    /// </summary>
    /// <param name="forumId">吧 ID</param>
    /// <param name="forumName">吧名</param>
    public void SetForumName(ulong forumId, string forumName)
    {
        _cache.Set(forumId.ToString(), forumName);
        _cache.Set(forumName, forumId);
    }
}
