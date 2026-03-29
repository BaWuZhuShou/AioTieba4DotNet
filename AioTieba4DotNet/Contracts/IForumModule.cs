using AioTieba4DotNet.Models.Forums;

namespace AioTieba4DotNet;

public interface IForumModule
{
    Task<ulong> GetFidAsync(string fname, CancellationToken cancellationToken = default);

    Task<string> GetFnameAsync(ulong fid, CancellationToken cancellationToken = default);

    Task<ForumDetail> GetDetailAsync(ulong fid, CancellationToken cancellationToken = default);

    Task<ForumDetail> GetDetailAsync(string fname, CancellationToken cancellationToken = default);

    Task<bool> LikeAsync(string fname, CancellationToken cancellationToken = default);

    Task<bool> UnlikeAsync(string fname, CancellationToken cancellationToken = default);

    Task<bool> SignAsync(string fname, CancellationToken cancellationToken = default);

    Task<Forum> GetForumAsync(string fname, CancellationToken cancellationToken = default);

    Task<bool> DelBaWuAsync(string fname, string portrait, string baWuType,
        CancellationToken cancellationToken = default);
}
