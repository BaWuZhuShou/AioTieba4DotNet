using AioTieba4DotNet.Api.GetForumDetail.Entities;
using AioTieba4DotNet.Api.Entities.Contents;
using AioTieba4DotNet.Api.GetThreads.Entities;
using AioTieba4DotNet.Api.GetThreadPosts.Entities;
using AioTieba4DotNet.Api.GetComments.Entities;
using AioTieba4DotNet.Api.GetUInfoGetUserInfoApp.Entities;
using AioTieba4DotNet.Api.Profile.GetUInfoProfile.Entities;
using AioTieba4DotNet.Enums;

namespace AioTieba4DotNet.Abstractions;

public interface IForumModule
{
    Task<ulong> GetFidAsync(string fname);
    Task<string> GetFnameAsync(ulong fid);
    Task<ForumDetail> GetDetailAsync(ulong fid);
    Task<ForumDetail> GetDetailAsync(string fname);

    Task<bool> LikeAsync(string fname);
    Task<bool> UnlikeAsync(string fname);
    Task<bool> SignAsync(string fname);
}

public interface IThreadModule
{
    TiebaRequestMode RequestMode { get; set; }
    Task<Threads> GetThreadsAsync(string fname, int pn = 1, int rn = 30, ThreadSortType sort = ThreadSortType.Reply, bool isGood = false, TiebaRequestMode? mode = null);
    Task<Threads> GetThreadsAsync(ulong fid, int pn = 1, int rn = 30, ThreadSortType sort = ThreadSortType.Reply, bool isGood = false, TiebaRequestMode? mode = null);

    Task<Posts> GetPostsAsync(long tid, int pn = 1, int rn = 30, PostSortType sort = PostSortType.Asc, bool onlyThreadAuthor = false, bool withComments = false, int commentRn = 0, bool commentSortByAgree = false, TiebaRequestMode? mode = null);
    Task<AioTieba4DotNet.Api.GetComments.Entities.Comments> GetCommentsAsync(long tid, long pid, int pn = 1, bool isComment = false, TiebaRequestMode? mode = null);
    
    Task<bool> AgreeAsync(long tid, long pid = 0, bool isComment = false, bool isDisagree = false, bool isUndo = false);
    Task<bool> DisagreeAsync(long tid, long pid = 0, bool isComment = false, bool isUndo = false);
    Task<bool> UnagreeAsync(long tid, long pid = 0, bool isComment = false);
    Task<bool> UndisagreeAsync(long tid, long pid = 0, bool isComment = false);

    Task<long> AddThreadAsync(string fname, string title, string content);
    Task<long> AddThreadAsync(string fname, string title, List<IFrag> contents);

    Task<long> AddPostAsync(string fname, long tid, string content, long quoteId = 0, uint floor = 0);
    Task<long> AddPostAsync(string fname, long tid, List<IFrag> contents, long quoteId = 0, uint floor = 0);

    Task<bool> DelThreadAsync(string fname, long tid);
    Task<bool> DelPostAsync(string fname, long tid, long pid);
}

public interface IUserModule
{
    Task<string> GetTbsAsync();
    Task<UserInfoGuInfoApp> GetBasicInfoAsync(int userId);
    Task<UserInfoPf> GetProfileAsync(int userId);
    Task<UserInfoPf> GetProfileAsync(string portraitOrUserName);
    Task<bool> BlockAsync(ulong fid, string portrait, int day = 1, string reason = "");
    Task<bool> BlockAsync(string fname, string portrait, int day = 1, string reason = "");
    Task<bool> FollowAsync(string portrait);
    Task<bool> UnfollowAsync(string portrait);
}


public interface ITiebaClient
{
    TiebaRequestMode RequestMode { get; set; }
    ITiebaHttpCore HttpCore { get; }
    IForumModule Forums { get; }
    IThreadModule Threads { get; }
    IUserModule Users { get; }
    ITiebaWsCore WsCore { get; }
}
