using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.Block;
using AioTieba4DotNet.Api.GetFollows;
using AioTieba4DotNet.Api.GetUInfoGetUserInfoApp;
using AioTieba4DotNet.Api.GetUInfoGetUserInfoApp.Entities;
using AioTieba4DotNet.Api.GetUInfoPanel;
using AioTieba4DotNet.Api.GetUInfoPanel.Entities;
using AioTieba4DotNet.Api.GetUInfoUserJson;
using AioTieba4DotNet.Api.GetUInfoUserJson.Entities;
using AioTieba4DotNet.Api.Login;
using AioTieba4DotNet.Api.Login.Entities;
using AioTieba4DotNet.Api.Profile.GetUInfoProfile;
using AioTieba4DotNet.Api.Profile.GetUInfoProfile.Entities;
using AioTieba4DotNet.Api.GetTbs;
using AioTieba4DotNet.Api.FollowUser;
using AioTieba4DotNet.Api.UnfollowUser;
using AioTieba4DotNet.Api.GetUserContents;
using AioTieba4DotNet.Api.GetUserContents.Entities;
using AioTieba4DotNet.Api.Entities;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Enums;

namespace AioTieba4DotNet.Modules;

/// <summary>
/// 用户相关功能模块
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
/// <param name="forumModule">贴吧模块组件</param>
/// <param name="wsCore">Websocket 核心组件</param>
public class UserModule(ITiebaHttpCore httpCore, IForumModule forumModule, ITiebaWsCore wsCore) : IUserModule
{
    /// <summary>
    /// 默认请求模式 (Http/Websocket)
    /// </summary>
    public TiebaRequestMode RequestMode { get; set; } = TiebaRequestMode.Http;

    /// <summary>
    /// 获取 TBS 校验码
    /// </summary>
    /// <returns>TBS 字符串</returns>
    public async Task<string> GetTbsAsync()
    {
        var api = new GetTbs(httpCore);
        return await api.RequestAsync();
    }

    /// <summary>
    /// 获取用户基础信息 (App端接口)
    /// </summary>
    /// <param name="userId">用户 ID (uid)</param>
    /// <returns>用户基础信息</returns>
    public async Task<UserInfoGuInfoApp> GetBasicInfoAsync(int userId)
    {
        var api = new GetUInfoGetUserInfoApp((HttpCore)httpCore);
        return await api.RequestAsync(userId);
    }

    /// <summary>
    /// 获取用户详细主页信息 (通过用户 ID)
    /// </summary>
    /// <param name="userId">用户 ID (uid)</param>
    /// <returns>用户主页信息</returns>
    public async Task<UserInfoPf> GetProfileAsync(int userId)
    {
        var api = new GetUInfoProfile<int>((HttpCore)httpCore);
        return await api.RequestAsync(userId);
    }

    /// <summary>
    /// 获取用户详细主页信息 (通过用户名或 Portrait)
    /// </summary>
    /// <param name="portraitOrUserName">用户名 (un) 或用户头像 ID (portrait)</param>
    /// <returns>用户主页信息</returns>
    public async Task<UserInfoPf> GetProfileAsync(string portraitOrUserName)
    {
        var api = new GetUInfoProfile<string>((HttpCore)httpCore);
        return await api.RequestAsync(portraitOrUserName);
    }

    /// <summary>
    /// 封禁用户 (通过吧 ID)
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <param name="portrait">用户头像 ID (Portrait)</param>
    /// <param name="day">封禁天数</param>
    /// <param name="reason">封禁原因</param>
    /// <returns>操作是否成功</returns>
    public async Task<bool> BlockAsync(ulong fid, string portrait, int day = 1, string reason = "")
    {
        var api = new Block((HttpCore)httpCore);
        return await api.RequestAsync(fid, portrait, day, reason);
    }

    /// <summary>
    /// 封禁用户 (通过吧名)
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="portrait">用户头像 ID (Portrait)</param>
    /// <param name="day">封禁天数</param>
    /// <param name="reason">封禁原因</param>
    /// <returns>操作是否成功</returns>
    public async Task<bool> BlockAsync(string fname, string portrait, int day = 1, string reason = "")
    {
        var fid = await forumModule.GetFidAsync(fname);
        return await BlockAsync(fid, portrait, day, reason);
    }

    /// <summary>
    /// 关注用户
    /// </summary>
    /// <param name="portrait">用户头像 ID (Portrait)</param>
    /// <returns>操作是否成功</returns>
    public async Task<bool> FollowAsync(string portrait)
    {
        var api = new FollowUser(httpCore);
        return await api.RequestAsync(portrait);
    }

    /// <summary>
    /// 取消关注用户
    /// </summary>
    /// <param name="portrait">用户头像 ID (Portrait)</param>
    /// <returns>操作是否成功</returns>
    public async Task<bool> UnfollowAsync(string portrait)
    {
        var api = new UnfollowUser(httpCore);
        return await api.RequestAsync(portrait);
    }

    /// <summary>
    /// 获取用户关注列表
    /// </summary>
    /// <param name="userId">用户 ID (uid)</param>
    /// <param name="pn">页码</param>
    /// <returns>关注的用户列表</returns>
    public async Task<UserList> GetFollowsAsync(long userId, int pn = 1)
    {
        var api = new GetFollows(httpCore);
        return await api.RequestAsync(userId, pn);
    }

    /// <summary>
    /// 获取用户面板信息
    /// </summary>
    /// <param name="nameOrPortrait">用户名或 Portrait</param>
    /// <returns>用户面板信息</returns>
    public async Task<UserInfoPanel> GetPanelInfoAsync(string nameOrPortrait)
    {
        var api = new GetUInfoPanel(httpCore);
        return await api.RequestAsync(nameOrPortrait);
    }

    /// <summary>
    /// 获取用户信息 JSON
    /// </summary>
    /// <param name="username">用户名</param>
    /// <returns>用户信息实体</returns>
    public async Task<UserInfoJson> GetUserInfoJsonAsync(string username)
    {
        var api = new GetUInfoUserJson(httpCore);
        return await api.RequestAsync(username);
    }

    /// <summary>
    /// 登录并获取用户信息和 TBS
    /// </summary>
    /// <returns>包含登录用户信息和 TBS 的元组</returns>
    public async Task<(UserInfoLogin User, string Tbs)> LoginAsync()
    {
        var api = new Login(httpCore);
        return await api.RequestAsync();
    }

    /// <summary>
    /// 获取用户发布的回复列表
    /// </summary>
    /// <param name="userId">用户 ID (uid)</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页条数</param>
    /// <param name="version">客户端版本</param>
    /// <param name="mode">请求模式覆盖（可选）</param>
    /// <returns>回复列表实体</returns>
    public async Task<UserPostss> GetPostsAsync(int userId, uint pn = 1, uint rn = 20, string version = "8.9.8.5",
        TiebaRequestMode? mode = null)
    {
        var api = new GetPosts(httpCore, wsCore, mode ?? RequestMode);
        return await api.RequestAsync(userId, pn, rn, version);
    }

    /// <summary>
    /// 获取用户发布的主题帖列表
    /// </summary>
    /// <param name="userId">用户 ID (uid)</param>
    /// <param name="pn">页码</param>
    /// <param name="publicOnly">是否只获取公开贴子</param>
    /// <param name="mode">请求模式覆盖（可选）</param>
    /// <returns>主题帖列表实体</returns>
    public async Task<UserThreads> GetThreadsAsync(int userId, uint pn = 1, bool publicOnly = true,
        TiebaRequestMode? mode = null)
    {
        var api = new GetUserThreads(httpCore, wsCore, mode ?? RequestMode);
        return await api.RequestAsync(userId, pn, publicOnly);
    }
}
