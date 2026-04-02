using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Forums;

namespace AioTieba4DotNet.Contracts;

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
    /// <returns>贴吧详情 <see cref="ForumDetail" /></returns>
    Task<ForumDetail> GetDetailAsync(ulong fid, CancellationToken cancellationToken = default);

    /// <summary>
    ///     按吧名获取贴吧详情
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>贴吧详情 <see cref="ForumDetail" /></returns>
    Task<ForumDetail> GetDetailAsync(string fname, CancellationToken cancellationToken = default);

    /// <summary>
    ///     关注贴吧
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> FollowAsync(ulong fid, CancellationToken cancellationToken = default);

    /// <summary>
    ///     关注贴吧
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> FollowAsync(string fname, CancellationToken cancellationToken = default);

    /// <summary>
    ///     取消关注贴吧
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UnfollowAsync(ulong fid, CancellationToken cancellationToken = default);

    /// <summary>
    ///     取消关注贴吧
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UnfollowAsync(string fname, CancellationToken cancellationToken = default);

    /// <summary>
    ///     签到
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> SignAsync(string fname, CancellationToken cancellationToken = default);

    /// <summary>
    ///     一键签到当前账号关注的贴吧
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> SignForumsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     完成签到成长任务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> SignGrowthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取贴吧信息
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>贴吧信息 <see cref="Forum" /></returns>
    Task<Forum> GetForumAsync(string fname, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户关注贴吧列表
    /// </summary>
    /// <param name="userId">目标用户 ID</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>关注贴吧列表 <see cref="FollowForums" /></returns>
    Task<FollowForums> GetFollowForumsAsync(long userId, int pn = 1, int rn = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取当前账号关注贴吧列表
    /// </summary>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>当前账号关注贴吧列表 <see cref="SelfFollowForums" />，包含签到状态</returns>
    Task<SelfFollowForums> GetSelfFollowForumsAsync(int pn = 1, int rn = 200,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取当前账号关注贴吧 V1 列表
    /// </summary>
    /// <remarks>
    ///     该方法对应 aiotieba `get_self_follow_forums_v1`。<c>V1</c> 用于标识与 <see cref="GetSelfFollowForumsAsync" /> 并列支持的 V1
    ///     这一组接口，而不是泛指分页能力。
    /// </remarks>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>V1 关注贴吧列表 <see cref="SelfFollowForumsV1" />，保留独立分页返回形状</returns>
    Task<SelfFollowForumsV1> GetSelfFollowForumsV1Async(int pn = 1, int rn = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     按吧名获取精华分类 ID
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="cname">精华分类名；空白值直接返回 0</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>精华分类 ID；未命中时返回 0</returns>
    Task<int> GetCidAsync(string fname, string cname = "", CancellationToken cancellationToken = default);

    /// <summary>
    ///     按吧 ID 获取精华分类 ID
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <param name="cname">精华分类名；空白值直接返回 0</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>精华分类 ID；未命中时返回 0</returns>
    Task<int> GetCidAsync(ulong fid, string cname = "", CancellationToken cancellationToken = default);

    /// <summary>
    ///     按 URL 获取图片原始字节
    /// </summary>
    /// <param name="imageUrl">图片 URL</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>图片字节结果 <see cref="ForumImageBytes" /></returns>
    Task<ForumImageBytes> GetImageBytesAsync(string imageUrl, CancellationToken cancellationToken = default);

    /// <summary>
    ///     按 URL 获取图片
    /// </summary>
    /// <param name="imageUrl">图片 URL</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>图片结果 <see cref="ForumImage" /></returns>
    Task<ForumImage> GetImageAsync(string imageUrl, CancellationToken cancellationToken = default);

    /// <summary>
    ///     通过图片 hash 获取图片
    /// </summary>
    /// <param name="rawHash">图片 hash</param>
    /// <param name="size">图片尺寸</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>图片结果 <see cref="ForumImage" />；尺寸非法时返回空图片</returns>
    Task<ForumImage> GetImageByHashAsync(string rawHash, ForumImageSize size = ForumImageSize.Small,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     通过 portrait 获取头像
    /// </summary>
    /// <param name="portrait">portrait</param>
    /// <param name="size">头像尺寸</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>头像结果 <see cref="ForumImage" />；尺寸非法时返回空图片</returns>
    Task<ForumImage> GetPortraitAsync(string portrait, ForumImageSize size = ForumImageSize.Small,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     按吧名执行精确搜索
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="query">搜索文本</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页数量</param>
    /// <param name="searchType">搜索排序方式</param>
    /// <param name="onlyThread">是否仅搜索主题帖</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>搜索结果 <see cref="ExactSearches" /></returns>
    Task<ExactSearches> SearchExactAsync(string fname, string query, int pn = 1, int rn = 30,
        ForumSearchType searchType = ForumSearchType.All, bool onlyThread = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     按吧 ID 执行精确搜索
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <param name="query">搜索文本</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页数量</param>
    /// <param name="searchType">搜索排序方式</param>
    /// <param name="onlyThread">是否仅搜索主题帖</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>搜索结果 <see cref="ExactSearches" /></returns>
    Task<ExactSearches> SearchExactAsync(ulong fid, string query, int pn = 1, int rn = 30,
        ForumSearchType searchType = ForumSearchType.All, bool onlyThread = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取带最后回复人的首页帖子
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页数量</param>
    /// <param name="sort">排序方式</param>
    /// <param name="isGood">是否精品区</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>帖子列表 <see cref="LastReplyers" /></returns>
    Task<LastReplyers> GetLastReplyersAsync(string fname, int pn = 1, int rn = 30,
        ThreadSortType sort = ThreadSortType.Reply, bool isGood = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     按吧 ID 获取带最后回复人的首页帖子
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页数量</param>
    /// <param name="sort">排序方式</param>
    /// <param name="isGood">是否精品区</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>帖子列表 <see cref="LastReplyers" /></returns>
    Task<LastReplyers> GetLastReplyersAsync(ulong fid, int pn = 1, int rn = 30,
        ThreadSortType sort = ThreadSortType.Reply, bool isGood = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取吧会员列表
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="pn">页码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>吧会员列表 <see cref="MemberUsers" /></returns>
    Task<MemberUsers> GetMemberUsersAsync(string fname, int pn = 1, CancellationToken cancellationToken = default);

    /// <summary>
    ///     按吧 ID 获取吧会员列表
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <param name="pn">页码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>吧会员列表 <see cref="MemberUsers" /></returns>
    Task<MemberUsers> GetMemberUsersAsync(ulong fid, int pn = 1, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取吧签到排行榜
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="pn">页码</param>
    /// <param name="rankType">榜单类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>排行榜 <see cref="RankForums" /></returns>
    Task<RankForums> GetRankForumsAsync(string fname, int pn = 1,
        ForumRankType rankType = ForumRankType.Weekly, CancellationToken cancellationToken = default);

    /// <summary>
    ///     按吧 ID 获取吧签到排行榜
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <param name="pn">页码</param>
    /// <param name="rankType">榜单类型</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>排行榜 <see cref="RankForums" /></returns>
    Task<RankForums> GetRankForumsAsync(ulong fid, int pn = 1,
        ForumRankType rankType = ForumRankType.Weekly, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取推荐配额状态
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>推荐配额状态 <see cref="RecomStatus" /></returns>
    Task<RecomStatus> GetRecomStatusAsync(string fname, CancellationToken cancellationToken = default);

    /// <summary>
    ///     按吧 ID 获取推荐配额状态
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>推荐配额状态 <see cref="RecomStatus" /></returns>
    Task<RecomStatus> GetRecomStatusAsync(ulong fid, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取吧广场列表
    /// </summary>
    /// <param name="cname">类别名</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>吧广场列表 <see cref="SquareForums" /></returns>
    Task<SquareForums> GetSquareForumsAsync(string cname, int pn = 1, int rn = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取贴吧统计数据
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>统计数据 <see cref="ForumStatistics" /></returns>
    Task<ForumStatistics> GetStatisticsAsync(string fname, CancellationToken cancellationToken = default);

    /// <summary>
    ///     按吧 ID 获取贴吧统计数据
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>统计数据 <see cref="ForumStatistics" /></returns>
    Task<ForumStatistics> GetStatisticsAsync(ulong fid, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取当前账号在某吧的等级信息
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>等级信息 <see cref="ForumLevelInfo" /></returns>
    Task<ForumLevelInfo> GetForumLevelAsync(string fname, CancellationToken cancellationToken = default);

    /// <summary>
    ///     按吧 ID 获取当前账号在某吧的等级信息
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>等级信息 <see cref="ForumLevelInfo" /></returns>
    Task<ForumLevelInfo> GetForumLevelAsync(ulong fid, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取指定贴吧的房间列表
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>房间列表 <see cref="RoomList" />；结果按 upstream 房间 JSON 扁平化为字典容器</returns>
    Task<RoomList> GetRoomListByFidAsync(ulong fid, CancellationToken cancellationToken = default);

    /// <summary>
    ///     屏蔽贴吧首页推荐
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> DislikeAsync(ulong fid, CancellationToken cancellationToken = default);

    /// <summary>
    ///     屏蔽贴吧首页推荐
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> DislikeAsync(string fname, CancellationToken cancellationToken = default);

    /// <summary>
    ///     解除贴吧首页推荐屏蔽
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UndislikeAsync(ulong fid, CancellationToken cancellationToken = default);

    /// <summary>
    ///     解除贴吧首页推荐屏蔽
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UndislikeAsync(string fname, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取首页推荐屏蔽贴吧列表
    /// </summary>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>首页推荐屏蔽贴吧列表 <see cref="DislikeForums" /></returns>
    Task<DislikeForums> GetDislikeForumsAsync(int pn = 1, int rn = 20,
        CancellationToken cancellationToken = default);
}
