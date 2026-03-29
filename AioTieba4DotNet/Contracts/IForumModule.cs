using AioTieba4DotNet.Models.Forums;

namespace AioTieba4DotNet;

/// <summary>
///     贴吧模块契约
/// </summary>
public interface IForumModule
{
    /// <summary>
    ///     获取贴吧 ID
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>贴吧 ID</returns>
    Task<ulong> GetFidAsync(string fname, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取贴吧名称
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>吧名</returns>
    Task<string> GetFnameAsync(ulong fid, CancellationToken cancellationToken = default);

    /// <summary>
    ///     按吧 ID 获取贴吧详情
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>贴吧详情 <see cref="ForumDetail"/></returns>
    Task<ForumDetail> GetDetailAsync(ulong fid, CancellationToken cancellationToken = default);

    /// <summary>
    ///     按吧名获取贴吧详情
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>贴吧详情 <see cref="ForumDetail"/></returns>
    Task<ForumDetail> GetDetailAsync(string fname, CancellationToken cancellationToken = default);

    /// <summary>
    ///     关注贴吧
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> LikeAsync(string fname, CancellationToken cancellationToken = default);

    /// <summary>
    ///     取消关注贴吧
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UnlikeAsync(string fname, CancellationToken cancellationToken = default);

    /// <summary>
    ///     签到
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> SignAsync(string fname, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取贴吧信息
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>贴吧信息 <see cref="Forum"/></returns>
    Task<Forum> GetForumAsync(string fname, CancellationToken cancellationToken = default);

    /// <summary>
    ///     移除吧务
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="portrait">用户 portrait</param>
    /// <param name="baWuType">吧务类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> DelBaWuAsync(string fname, string portrait, string baWuType,
        CancellationToken cancellationToken = default);
}
