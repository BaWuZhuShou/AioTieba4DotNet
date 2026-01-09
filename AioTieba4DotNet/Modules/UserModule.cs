using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.Block;
using AioTieba4DotNet.Api.GetUInfoGetUserInfoApp;
using AioTieba4DotNet.Api.GetUInfoGetUserInfoApp.Entities;
using AioTieba4DotNet.Api.Profile.GetUInfoProfile;
using AioTieba4DotNet.Api.Profile.GetUInfoProfile.Entities;
using AioTieba4DotNet.Api.GetTbs;
using AioTieba4DotNet.Api.FollowUser;
using AioTieba4DotNet.Api.UnfollowUser;
using AioTieba4DotNet.Core;

namespace AioTieba4DotNet.Modules;

public class UserModule(ITiebaHttpCore httpCore, IForumModule forumModule) : IUserModule
{
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
}
