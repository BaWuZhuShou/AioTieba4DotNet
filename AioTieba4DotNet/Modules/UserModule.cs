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

public class UserModule(ITiebaHttpCore httpCore, IForumModule forumModule, ITiebaWsCore wsCore) : IUserModule
{
    public TiebaRequestMode RequestMode { get; set; } = TiebaRequestMode.Http;

    public async Task<string> GetTbsAsync()
    {
        var api = new GetTbs(httpCore);
        return await api.RequestAsync();
    }

    public async Task<UserInfoGuInfoApp> GetBasicInfoAsync(int userId)
    {
        var api = new GetUInfoGetUserInfoApp((HttpCore)httpCore);
        return await api.RequestAsync(userId);
    }

    public async Task<UserInfoPf> GetProfileAsync(int userId)
    {
        var api = new GetUInfoProfile<int>((HttpCore)httpCore);
        return await api.RequestAsync(userId);
    }

    public async Task<UserInfoPf> GetProfileAsync(string portraitOrUserName)
    {
        var api = new GetUInfoProfile<string>((HttpCore)httpCore);
        return await api.RequestAsync(portraitOrUserName);
    }

    public async Task<bool> BlockAsync(ulong fid, string portrait, int day = 1, string reason = "")
    {
        var api = new Block((HttpCore)httpCore);
        return await api.RequestAsync(fid, portrait, day, reason);
    }

    public async Task<bool> BlockAsync(string fname, string portrait, int day = 1, string reason = "")
    {
        var fid = await forumModule.GetFidAsync(fname);
        return await BlockAsync(fid, portrait, day, reason);
    }

    public async Task<bool> FollowAsync(string portrait)
    {
        var api = new FollowUser(httpCore);
        return await api.RequestAsync(portrait);
    }

    public async Task<bool> UnfollowAsync(string portrait)
    {
        var api = new UnfollowUser(httpCore);
        return await api.RequestAsync(portrait);
    }

    public async Task<UserList> GetFollowsAsync(long userId, int pn = 1)
    {
        var api = new GetFollows(httpCore);
        return await api.RequestAsync(userId, pn);
    }

    public async Task<UserInfoPanel> GetPanelInfoAsync(string nameOrPortrait)
    {
        var api = new GetUInfoPanel(httpCore);
        return await api.RequestAsync(nameOrPortrait);
    }

    public async Task<UserInfoJson> GetUserInfoJsonAsync(string username)
    {
        var api = new GetUInfoUserJson(httpCore);
        return await api.RequestAsync(username);
    }

    public async Task<(UserInfoLogin User, string Tbs)> LoginAsync()
    {
        var api = new Login(httpCore);
        return await api.RequestAsync();
    }

    public async Task<UserPostss> GetPostsAsync(int userId, uint pn = 1, uint rn = 20, string version = "8.9.8.5", TiebaRequestMode? mode = null)
    {
        var api = new GetPosts(httpCore, wsCore, mode ?? RequestMode);
        return await api.RequestAsync(userId, pn, rn, version);
    }

    public async Task<UserThreads> GetThreadsAsync(int userId, uint pn = 1, bool publicOnly = true, TiebaRequestMode? mode = null)
    {
        var api = new GetUserThreads(httpCore, wsCore, mode ?? RequestMode);
        return await api.RequestAsync(userId, pn, publicOnly);
    }
}
