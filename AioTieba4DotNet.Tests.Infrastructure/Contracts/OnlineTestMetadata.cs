using System.Diagnostics.CodeAnalysis;

namespace AioTieba4DotNet.Tests.Infrastructure.Contracts;

[ExcludeFromCodeCoverage]
public static class OnlineTestContractCategories
{
    public const string Architecture = "Contract:Architecture";
    public const string Environment = "Contract:Environment";
    public const string Gating = "Contract:Gating";
    public const string ProjectLayout = "Contract:ProjectLayout";
    public const string Style = "Contract:Style";
    public const string Cleanup = "Contract:Cleanup";
    public const string CleanupFailure = "Contract:CleanupFailure";
    public const string RestrictedIsolation = "Contract:RestrictedIsolation";
    public const string ThreadWriteCleanupFailure = "Contract:ThreadWriteCleanupFailure";

    public static readonly string[] All =
    [
        Architecture,
        Environment,
        Gating,
        ProjectLayout,
        Style,
        Cleanup,
        CleanupFailure,
        RestrictedIsolation,
        ThreadWriteCleanupFailure
    ];
}

[ExcludeFromCodeCoverage]
public static class OnlineTestSuiteCategories
{
    public const string SafeOrdered = "Suite:SafeOrdered";
    public const string RestrictedOrdered = "Suite:RestrictedOrdered";

    public static readonly string[] All =
    [
        SafeOrdered,
        RestrictedOrdered
    ];
}

[ExcludeFromCodeCoverage]
public static class OnlineTestFeatureCategories
{
    public const string ForumFoundation = "Feature:ForumFoundation";
    public const string ForumExtensions = "Feature:ForumExtensions";
    public const string ThreadRead = "Feature:ThreadRead";
    public const string ThreadWrite = "Feature:ThreadWrite";
    public const string UserSocial = "Feature:UserSocial";
    public const string Messaging = "Feature:Messaging";
    public const string Moderation = "Feature:Moderation";
    public const string Admin = "Feature:Admin";

    public static readonly string[] All =
    [
        ForumFoundation,
        ForumExtensions,
        ThreadRead,
        ThreadWrite,
        UserSocial,
        Messaging,
        Moderation,
        Admin
    ];
}

[ExcludeFromCodeCoverage]
public static class OnlineTestTierCategories
{
    public const string Safe = "Tier:Safe";
    public const string Restricted = "Tier:Restricted";

    public static readonly string[] All =
    [
        Safe,
        Restricted
    ];
}

[ExcludeFromCodeCoverage]
public static class OnlineTestStageCategories
{
    public const string ForumFoundation = "Stage:01-ForumFoundation";
    public const string ForumExtensions = "Stage:02-ForumExtensions";
    public const string ThreadRead = "Stage:03-ThreadRead";
    public const string UserSocial = "Stage:04-UserSocial";
    public const string Messaging = "Stage:05-Messaging";
    public const string ThreadWrite = "Stage:06-ThreadWrite";
    public const string ModerationRestricted = "Stage:07-ModerationRestricted";
    public const string AdminRestricted = "Stage:08-AdminRestricted";

    public static readonly string[] All =
    [
        ForumFoundation,
        ForumExtensions,
        ThreadRead,
        UserSocial,
        Messaging,
        ThreadWrite,
        ModerationRestricted,
        AdminRestricted
    ];
}

[ExcludeFromCodeCoverage]
public static class OnlineTestCapabilityCategories
{
    public const string Authenticated = "Capability:Authenticated";
    public const string Messaging = "Capability:Messaging";
    public const string Moderation = "Capability:Moderation";
    public const string Admin = "Capability:Admin";

    public static readonly string[] All =
    [
        Authenticated,
        Messaging,
        Moderation,
        Admin
    ];
}

[ExcludeFromCodeCoverage]
public static class OnlineTestApiCategories
{
    public const string ForumsGetForumAsync = "Api:Forums.GetForumAsync";
    public const string ForumsGetDetailAsync = "Api:Forums.GetDetailAsync";
    public const string ForumsGetFnameAsync = "Api:Forums.GetFnameAsync";
    public const string ForumsSearchExactAsync = "Api:Forums.SearchExactAsync";
    public const string ForumsGetLastReplyersAsync = "Api:Forums.GetLastReplyersAsync";
    public const string ForumsGetRankForumsAsync = "Api:Forums.GetRankForumsAsync";
    public const string ForumsGetSelfFollowForumsAsync = "Api:Forums.GetSelfFollowForumsAsync";
    public const string ForumsFollowAsync = "Api:Forums.FollowAsync";
    public const string ForumsUnfollowAsync = "Api:Forums.UnfollowAsync";
    public const string ThreadsGetThreadsAsync = "Api:Threads.GetThreadsAsync";
    public const string ThreadsGetPostsAsync = "Api:Threads.GetPostsAsync";
    public const string ThreadsGetCommentsAsync = "Api:Threads.GetCommentsAsync";
    public const string ThreadsAddPostAsync = "Api:Threads.AddPostAsync";
    public const string ThreadsAgreeAsync = "Api:Threads.AgreeAsync";
    public const string ThreadsDelPostAsync = "Api:Threads.DelPostAsync";
    public const string ThreadsUnagreeAsync = "Api:Threads.UnagreeAsync";
    public const string ThreadsRecoverAsync = "Api:Threads.RecoverAsync";
    public const string UsersGetProfileAsync = "Api:Users.GetProfileAsync";
    public const string UsersGetUserInfoAppAsync = "Api:Users.GetUserInfoAppAsync";
    public const string UsersGetUserInfoWebAsync = "Api:Users.GetUserInfoWebAsync";
    public const string UsersGetHomepageAsync = "Api:Users.GetHomepageAsync";
    public const string UsersGetSelfInfoAsync = "Api:Users.GetSelfInfoAsync";
    public const string UsersGetFansAsync = "Api:Users.GetFansAsync";
    public const string UsersGetBlacklistAsync = "Api:Users.GetBlacklistAsync";
    public const string UsersGetBlacklistOldAsync = "Api:Users.GetBlacklistOldAsync";
    public const string UsersGetUserByTiebaUidAsync = "Api:Users.GetUserByTiebaUidAsync";
    public const string UsersGetThreadsAsync = "Api:Users.GetThreadsAsync";
    public const string UsersGetPostsAsync = "Api:Users.GetPostsAsync";
    public const string UsersGetUserForumInfoAsync = "Api:Users.GetUserForumInfoAsync";
    public const string UsersGetRankUsersAsync = "Api:Users.GetRankUsersAsync";
    public const string UsersGetPanelInfoAsync = "Api:Users.GetPanelInfoAsync";
    public const string MessagesGetAtsAsync = "Api:Messages.GetAtsAsync";
    public const string MessagesGetRepliesAsync = "Api:Messages.GetRepliesAsync";
    public const string MessagesGetGroupMessagesAsync = "Api:Messages.GetGroupMessagesAsync";
    public const string MessagesSendMessageAsync = "Api:Messages.SendMessageAsync";
    public const string AdminsGetBawuInfoAsync = "Api:Admins.GetBawuInfoAsync";
    public const string AdminsGetBlocksAsync = "Api:Admins.GetBlocksAsync";
    public const string AdminsBlockAsync = "Api:Admins.BlockAsync";
    public const string AdminsUnblockAsync = "Api:Admins.UnblockAsync";

    public static readonly string[] All =
    [
        ForumsGetForumAsync,
        ForumsGetDetailAsync,
        ForumsGetFnameAsync,
        ForumsSearchExactAsync,
        ForumsGetLastReplyersAsync,
        ForumsGetRankForumsAsync,
        ForumsGetSelfFollowForumsAsync,
        ForumsFollowAsync,
        ForumsUnfollowAsync,
        ThreadsGetThreadsAsync,
        ThreadsGetPostsAsync,
        ThreadsGetCommentsAsync,
        ThreadsAddPostAsync,
        ThreadsAgreeAsync,
        ThreadsDelPostAsync,
        ThreadsUnagreeAsync,
        ThreadsRecoverAsync,
        UsersGetProfileAsync,
        UsersGetUserInfoAppAsync,
        UsersGetUserInfoWebAsync,
        UsersGetHomepageAsync,
        UsersGetSelfInfoAsync,
        UsersGetFansAsync,
        UsersGetBlacklistAsync,
        UsersGetBlacklistOldAsync,
        UsersGetUserByTiebaUidAsync,
        UsersGetThreadsAsync,
        UsersGetPostsAsync,
        UsersGetUserForumInfoAsync,
        UsersGetRankUsersAsync,
        UsersGetPanelInfoAsync,
        MessagesGetAtsAsync,
        MessagesGetRepliesAsync,
        MessagesGetGroupMessagesAsync,
        MessagesSendMessageAsync,
        AdminsGetBawuInfoAsync,
        AdminsGetBlocksAsync,
        AdminsBlockAsync,
        AdminsUnblockAsync
    ];
}
