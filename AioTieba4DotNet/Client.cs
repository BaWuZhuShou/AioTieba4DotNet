using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.Block;
using AioTieba4DotNet.Api.Entities;
using AioTieba4DotNet.Api.GetFid;
using AioTieba4DotNet.Api.GetForumDetail;
using AioTieba4DotNet.Api.GetForumDetail.Entities;
using AioTieba4DotNet.Api.GetThreads;
using AioTieba4DotNet.Api.GetThreads.Entities;
using AioTieba4DotNet.Api.GetUInfoGetUserInfoApp;
using AioTieba4DotNet.Api.GetUInfoGetUserInfoApp.Entities;
using AioTieba4DotNet.Api.GetUInfoPanel;
using AioTieba4DotNet.Api.GetUInfoPanel.Entities;
using AioTieba4DotNet.Api.GetUInfoUserJson;
using AioTieba4DotNet.Api.GetUInfoUserJson.Entities;
using AioTieba4DotNet.Api.GetUserContents.Entities;
using AioTieba4DotNet.Api.Login;
using AioTieba4DotNet.Api.Login.Entities;
using AioTieba4DotNet.Api.Profile.GetUInfoProfile;
using AioTieba4DotNet.Api.Profile.GetUInfoProfile.Entities;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Entities;
using AioTieba4DotNet.Enums;

namespace AioTieba4DotNet;

/// <summary>
/// 客户端
/// </summary>
public class Client : TiebaClient
{
    private readonly ForumInfoCache _forumInfoCache = new();

    public ThreadSortType ThreadSortType { get; init; } = ThreadSortType.Reply;
    public bool ThreadIsGood { get; init; }
    public int ThreadRn { get; init; } = 30;

    [Obsolete("Use TiebaClient instead.")]
    public Client() : base(new HttpCore())
    {
    }

    [Obsolete("Use TiebaClient instead.")]
    public Client(string bduss, string stoken) : base(new HttpCore())
    {
        HttpCore.SetAccount(new Account(bduss, stoken));
    }

    [Obsolete("Use Forums.GetFidAsync instead.")]
    public Task<ulong> GetFid(string fname) => Forums.GetFidAsync(fname);

    [Obsolete("Use Forums.GetFnameAsync instead.")]
    public Task<string> GetFname(ulong fid) => Forums.GetFnameAsync(fid);

    [Obsolete("Use Forums.GetDetailAsync instead.")]
    public Task<ForumDetail> GetForumDetail(ulong fid) => Forums.GetDetailAsync(fid);

    [Obsolete("Use Forums.GetDetailAsync instead.")]
    public Task<ForumDetail> GetForumDetail(string fname) => Forums.GetDetailAsync(fname);

    [Obsolete("Use Threads.GetThreadsAsync instead.")]
    public Task<Threads> GetThreads(string fname, int pn, int rn, ThreadSortType sort, bool isGood) 
        => Threads.GetThreadsAsync(fname, pn, rn, sort, isGood);

    [Obsolete("Use Threads.GetThreadsAsync instead.")]
    public Task<Threads> GetThreads(ulong fid, int pn, int rn, ThreadSortType sort, bool isGood)
        => Threads.GetThreadsAsync(fid, pn, rn, sort, isGood);

    [Obsolete("Use Threads.GetThreadsAsync instead.")]
    public Task<Threads> GetThreads(string fname, int pn) 
        => Threads.GetThreadsAsync(fname, pn, ThreadRn, ThreadSortType, ThreadIsGood);

    [Obsolete("Use Threads.GetThreadsAsync instead.")]
    public Task<Threads> GetThreads(ulong fid, int pn) 
        => Threads.GetThreadsAsync(fid, pn, ThreadRn, ThreadSortType, ThreadIsGood);

    [Obsolete("Use Users.GetProfileAsync instead.")]
    public Task<UserInfoPf> GetUserInfo(int userId) => Users.GetProfileAsync(userId);

    [Obsolete("Use Users.GetProfileAsync instead.")]
    public Task<UserInfoPf> GetUserInfo(string userNameOrPortrait) => Users.GetProfileAsync(userNameOrPortrait);

    [Obsolete("Use Users.BlockAsync instead.")]
    public Task<bool> Block(ulong fid, string portrait, int day = 1, string reason = "") 
        => Users.BlockAsync(fid, portrait, day, reason);

    [Obsolete("Use Users.BlockAsync instead.")]
    public Task<bool> Block(string fname, string portrait, int day = 1, string reason = "") 
        => Users.BlockAsync(fname, portrait, day, reason);
}