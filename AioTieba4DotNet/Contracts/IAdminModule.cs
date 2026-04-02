using AioTieba4DotNet.Models.Admins;

namespace AioTieba4DotNet.Contracts;

/// <summary>
///     吧务 / 后台管理模块契约
/// </summary>
public interface IAdminModule
{
    /// <summary>
    ///     添加吧务
    /// </summary>
    /// <remarks>
    ///     该方法保留 `Bawu` 作为公开根名称，并直接对应 upstream `add_bawu` family。
    /// </remarks>
    /// <param name="fname">吧名</param>
    /// <param name="userName">目标用户名</param>
    /// <param name="bawuType">吧务类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> AddBawuAsync(string fname, string userName, BawuType bawuType,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     删除吧务
    /// </summary>
    /// <remarks>
    ///     该方法保留 `Bawu` 作为公开根名称，并直接对应 upstream `del_bawu`。
    /// </remarks>
    /// <param name="fname">吧名</param>
    /// <param name="portrait">目标用户 portrait</param>
    /// <param name="bawuType">吧务类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> DelBawuAsync(string fname, string portrait, BawuType bawuType,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     添加吧务黑名单
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="userId">目标用户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> AddBawuBlacklistAsync(string fname, long userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     移除吧务黑名单
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="userId">目标用户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> DelBawuBlacklistAsync(string fname, long userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取吧务黑名单列表
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="pn">页码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>吧务黑名单列表 <see cref="BawuBlacklistUsers" /></returns>
    Task<BawuBlacklistUsers> GetBawuBlacklistAsync(string fname, int pn = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取吧务团队信息
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>吧务团队信息 <see cref="BawuInfo" /></returns>
    Task<BawuInfo> GetBawuInfoAsync(string fname, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取目标吧务当前权限
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="portrait">目标用户 portrait</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>吧务权限 <see cref="BawuPerm" /></returns>
    Task<BawuPerm> GetBawuPermAsync(string fname, string portrait, CancellationToken cancellationToken = default);

    /// <summary>
    ///     设置目标吧务权限
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="portrait">目标用户 portrait</param>
    /// <param name="permissions">权限集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> SetBawuPermAsync(string fname, string portrait, BawuPermType permissions,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取吧务删帖日志
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="options">查询选项；为 <see langword="null" /> 时使用默认值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删帖日志 <see cref="BawuPostLogs" /></returns>
    Task<BawuPostLogs> GetBawuPostLogsAsync(string fname, BawuPostLogQueryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取吧务用户管理日志
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="options">查询选项；为 <see langword="null" /> 时使用默认值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户管理日志 <see cref="BawuUserLogs" /></returns>
    Task<BawuUserLogs> GetBawuUserLogsAsync(string fname, BawuUserLogQueryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取解封申诉列表
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>解封申诉列表 <see cref="Appeals" /></returns>
    Task<Appeals> GetUnblockAppealsAsync(string fname, int pn = 1, int rn = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     批量处理解封申诉
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="appealIds">申诉 ID 列表</param>
    /// <param name="refuse">是否拒绝；否则为同意</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> HandleUnblockAppealsAsync(string fname, IReadOnlyList<long> appealIds, bool refuse = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取封禁列表
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="userName">目标用户名筛选；空白值表示不筛选</param>
    /// <param name="pn">页码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>封禁列表 <see cref="Blocks" /></returns>
    Task<Blocks> GetBlocksAsync(string fname, string userName = "", int pn = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     封禁用户
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="portrait">目标用户 portrait</param>
    /// <param name="day">封禁天数</param>
    /// <param name="reason">封禁原因</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> BlockAsync(string fname, string portrait, int day = 1, string reason = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     解除封禁
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="userId">目标用户 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UnblockAsync(string fname, long userId, CancellationToken cancellationToken = default);
}
