using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace AioTieba4DotNet.Tests.Platform.Contracts;

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
public static class OnlineTestParityCategories
{
    public const string TruthFreeze = "Parity:TruthFreeze";
    public const string EvidenceSchema = "Parity:EvidenceSchema";
    public const string Gaps = "Parity:Gaps";
    public const string Wire = "Parity:Wire";
    public const string Signature = "Parity:Signature";
    public const string State = "Parity:State";
    public const string SharedSeams = "Parity:SharedSeams";
    public const string Forums = "Parity:Forums";
    public const string Threads = "Parity:Threads";
    public const string Users = "Parity:Users";
    public const string Messages = "Parity:Messages";
    public const string ClientLifecycle = "Parity:ClientLifecycle";
    public const string Admins = "Parity:Admins";

    public static readonly string[] All =
    [
        TruthFreeze,
        EvidenceSchema,
        Gaps,
        Wire,
        Signature,
        State,
        SharedSeams,
        Forums,
        Threads,
        Users,
        Messages,
        ClientLifecycle,
        Admins
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
    public const string FirstClassNamingRule = "Api:<Module>.<Method>";

    private static readonly Regex FirstClassCategoryPattern = new(
        @"^Api:(?<module>[A-Z][A-Za-z0-9]*)\.(?<method>[A-Z][A-Za-z0-9]*)$",
        RegexOptions.CultureInvariant);

    public const string ClientInitWebSocketAsync = "Api:Client.InitWebSocketAsync";
    public const string ClientInitZIdAsync = "Api:Client.InitZIdAsync";
    public const string ClientSyncAsync = "Api:Client.SyncAsync";
    public const string ForumsGetForumAsync = "Api:Forums.GetForumAsync";
    public const string ForumsGetDetailAsync = "Api:Forums.GetDetailAsync";
    public const string ForumsGetFidAsync = "Api:Forums.GetFidAsync";
    public const string ForumsGetFnameAsync = "Api:Forums.GetFnameAsync";
    public const string ForumsGetFollowForumsAsync = "Api:Forums.GetFollowForumsAsync";
    public const string ForumsGetSelfFollowForumsV1Async = "Api:Forums.GetSelfFollowForumsV1Async";
    public const string ForumsGetCidAsync = "Api:Forums.GetCidAsync";
    public const string ForumsGetImageBytesAsync = "Api:Forums.GetImageBytesAsync";
    public const string ForumsGetImageAsync = "Api:Forums.GetImageAsync";
    public const string ForumsGetImageByHashAsync = "Api:Forums.GetImageByHashAsync";
    public const string ForumsGetPortraitAsync = "Api:Forums.GetPortraitAsync";
    public const string ForumsSearchExactAsync = "Api:Forums.SearchExactAsync";
    public const string ForumsGetLastReplyersAsync = "Api:Forums.GetLastReplyersAsync";
    public const string ForumsGetMemberUsersAsync = "Api:Forums.GetMemberUsersAsync";
    public const string ForumsGetRankForumsAsync = "Api:Forums.GetRankForumsAsync";
    public const string ForumsGetRecomStatusAsync = "Api:Forums.GetRecomStatusAsync";
    public const string ForumsGetSquareForumsAsync = "Api:Forums.GetSquareForumsAsync";
    public const string ForumsGetStatisticsAsync = "Api:Forums.GetStatisticsAsync";
    public const string ForumsGetForumLevelAsync = "Api:Forums.GetForumLevelAsync";
    public const string ForumsGetRoomListByFidAsync = "Api:Forums.GetRoomListByFidAsync";
    public const string ForumsGetSelfFollowForumsAsync = "Api:Forums.GetSelfFollowForumsAsync";
    public const string ForumsFollowAsync = "Api:Forums.FollowAsync";
    public const string ForumsUnfollowAsync = "Api:Forums.UnfollowAsync";
    public const string ForumsDislikeAsync = "Api:Forums.DislikeAsync";
    public const string ForumsUndislikeAsync = "Api:Forums.UndislikeAsync";
    public const string ForumsGetDislikeForumsAsync = "Api:Forums.GetDislikeForumsAsync";
    public const string ThreadsGetThreadsAsync = "Api:Threads.GetThreadsAsync";
    public const string ThreadsGetPostsAsync = "Api:Threads.GetPostsAsync";
    public const string ThreadsGetCommentsAsync = "Api:Threads.GetCommentsAsync";
    public const string ThreadsGetRecoversAsync = "Api:Threads.GetRecoversAsync";
    public const string ThreadsGetRecoverInfoAsync = "Api:Threads.GetRecoverInfoAsync";
    public const string ThreadsGetTabMapAsync = "Api:Threads.GetTabMapAsync";
    public const string ThreadsAddPostAsync = "Api:Threads.AddPostAsync";
    public const string ThreadsAgreeAsync = "Api:Threads.AgreeAsync";
    public const string ThreadsDisagreeAsync = "Api:Threads.DisagreeAsync";
    public const string ThreadsDelPostAsync = "Api:Threads.DelPostAsync";
    public const string ThreadsDelThreadAsync = "Api:Threads.DelThreadAsync";
    public const string ThreadsDelThreadsAsync = "Api:Threads.DelThreadsAsync";
    public const string ThreadsDelPostsAsync = "Api:Threads.DelPostsAsync";
    public const string ThreadsGoodAsync = "Api:Threads.GoodAsync";
    public const string ThreadsUngoodAsync = "Api:Threads.UngoodAsync";
    public const string ThreadsTopAsync = "Api:Threads.TopAsync";
    public const string ThreadsUntopAsync = "Api:Threads.UntopAsync";
    public const string ThreadsRecommendAsync = "Api:Threads.RecommendAsync";
    public const string ThreadsUnagreeAsync = "Api:Threads.UnagreeAsync";
    public const string ThreadsUndisagreeAsync = "Api:Threads.UndisagreeAsync";
    public const string ThreadsRecoverAsync = "Api:Threads.RecoverAsync";
    public const string ThreadsSetThreadPrivacyAsync = "Api:Threads.SetThreadPrivacyAsync";
    public const string UsersGetTbsAsync = "Api:Users.GetTbsAsync";
    public const string UsersGetProfileAsync = "Api:Users.GetProfileAsync";
    public const string UsersFollowAsync = "Api:Users.FollowAsync";
    public const string UsersUnfollowAsync = "Api:Users.UnfollowAsync";
    public const string UsersGetFollowsAsync = "Api:Users.GetFollowsAsync";
    public const string UsersGetUserInfoAppAsync = "Api:Users.GetUserInfoAppAsync";
    public const string UsersGetUserInfoWebAsync = "Api:Users.GetUserInfoWebAsync";
    public const string UsersGetHomepageAsync = "Api:Users.GetHomepageAsync";
    public const string UsersGetUserInfoJsonAsync = "Api:Users.GetUserInfoJsonAsync";
    public const string UsersGetSelfInfoAsync = "Api:Users.GetSelfInfoAsync";
    public const string UsersGetSelfInfoInitNicknameAsync = "Api:Users.GetSelfInfoInitNicknameAsync";
    public const string UsersGetSelfInfoMoIndexAsync = "Api:Users.GetSelfInfoMoIndexAsync";
    public const string UsersLoginAsync = "Api:Users.LoginAsync";
    public const string UsersGetFansAsync = "Api:Users.GetFansAsync";
    public const string UsersGetBlacklistAsync = "Api:Users.GetBlacklistAsync";
    public const string UsersGetBlacklistOldAsync = "Api:Users.GetBlacklistOldAsync";
    public const string UsersSetBlacklistAsync = "Api:Users.SetBlacklistAsync";
    public const string UsersAddBlacklistOldAsync = "Api:Users.AddBlacklistOldAsync";
    public const string UsersRemoveBlacklistOldAsync = "Api:Users.RemoveBlacklistOldAsync";
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
    public const string AdminsGetBawuBlacklistAsync = "Api:Admins.GetBawuBlacklistAsync";
    public const string AdminsGetBawuInfoAsync = "Api:Admins.GetBawuInfoAsync";
    public const string AdminsGetBawuPermAsync = "Api:Admins.GetBawuPermAsync";
    public const string AdminsGetBawuPostLogsAsync = "Api:Admins.GetBawuPostLogsAsync";
    public const string AdminsGetBawuUserLogsAsync = "Api:Admins.GetBawuUserLogsAsync";
    public const string AdminsGetUnblockAppealsAsync = "Api:Admins.GetUnblockAppealsAsync";
    public const string AdminsGetBlocksAsync = "Api:Admins.GetBlocksAsync";
    public const string AdminsBlockAsync = "Api:Admins.BlockAsync";
    public const string AdminsUnblockAsync = "Api:Admins.UnblockAsync";

    public static readonly string[] All =
    [
        ClientInitWebSocketAsync,
        ClientInitZIdAsync,
        ClientSyncAsync,
        ForumsGetForumAsync,
        ForumsGetDetailAsync,
        ForumsGetFidAsync,
        ForumsGetFnameAsync,
        ForumsGetFollowForumsAsync,
        ForumsGetSelfFollowForumsV1Async,
        ForumsGetCidAsync,
        ForumsGetImageBytesAsync,
        ForumsGetImageAsync,
        ForumsGetImageByHashAsync,
        ForumsGetPortraitAsync,
        ForumsSearchExactAsync,
        ForumsGetLastReplyersAsync,
        ForumsGetMemberUsersAsync,
        ForumsGetRankForumsAsync,
        ForumsGetRecomStatusAsync,
        ForumsGetSquareForumsAsync,
        ForumsGetStatisticsAsync,
        ForumsGetForumLevelAsync,
        ForumsGetRoomListByFidAsync,
        ForumsGetSelfFollowForumsAsync,
        ForumsFollowAsync,
        ForumsUnfollowAsync,
        ForumsDislikeAsync,
        ForumsUndislikeAsync,
        ForumsGetDislikeForumsAsync,
        ThreadsGetThreadsAsync,
        ThreadsGetPostsAsync,
        ThreadsGetCommentsAsync,
        ThreadsGetRecoversAsync,
        ThreadsGetRecoverInfoAsync,
        ThreadsGetTabMapAsync,
        ThreadsAddPostAsync,
        ThreadsAgreeAsync,
        ThreadsDisagreeAsync,
        ThreadsDelPostAsync,
        ThreadsDelThreadAsync,
        ThreadsDelThreadsAsync,
        ThreadsDelPostsAsync,
        ThreadsGoodAsync,
        ThreadsUngoodAsync,
        ThreadsTopAsync,
        ThreadsUntopAsync,
        ThreadsRecommendAsync,
        ThreadsUnagreeAsync,
        ThreadsUndisagreeAsync,
        ThreadsRecoverAsync,
        ThreadsSetThreadPrivacyAsync,
        UsersGetTbsAsync,
        UsersGetProfileAsync,
        UsersFollowAsync,
        UsersUnfollowAsync,
        UsersGetFollowsAsync,
        UsersGetUserInfoAppAsync,
        UsersGetUserInfoWebAsync,
        UsersGetHomepageAsync,
        UsersGetUserInfoJsonAsync,
        UsersGetSelfInfoAsync,
        UsersGetSelfInfoInitNicknameAsync,
        UsersGetSelfInfoMoIndexAsync,
        UsersLoginAsync,
        UsersGetFansAsync,
        UsersGetBlacklistAsync,
        UsersGetBlacklistOldAsync,
        UsersSetBlacklistAsync,
        UsersAddBlacklistOldAsync,
        UsersRemoveBlacklistOldAsync,
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
        AdminsGetBawuBlacklistAsync,
        AdminsGetBawuInfoAsync,
        AdminsGetBawuPermAsync,
        AdminsGetBawuPostLogsAsync,
        AdminsGetBawuUserLogsAsync,
        AdminsGetUnblockAppealsAsync,
        AdminsGetBlocksAsync,
        AdminsBlockAsync,
        AdminsUnblockAsync,
    ];

    public static bool IsWellFormedFirstClassCategory(string category)
    {
        return category is not null && FirstClassCategoryPattern.IsMatch(category);
    }

    public static string CreateFirstClassCategory(string moduleName, string methodName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

        var category = $"Api:{moduleName}.{methodName}";
        if (!IsWellFormedFirstClassCategory(category))
            throw new ArgumentException(
                $"First-class API category '{category}' must follow the '{FirstClassNamingRule}' naming rule.",
                nameof(moduleName));

        return category;
    }
}
