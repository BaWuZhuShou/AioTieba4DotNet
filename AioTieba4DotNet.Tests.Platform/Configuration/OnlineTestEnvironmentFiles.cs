#nullable enable
using System;
using System.IO;

namespace AioTieba4DotNet.Tests.Platform.Configuration;

public static class OnlineTestEnvironmentFiles
{
    public const string SafeTemplateRelativePath = "AioTieba4DotNet.Tests.Platform/online-test.safe.template.json";
    public const string RestrictedTemplateRelativePath = "AioTieba4DotNet.Tests.Platform/online-test.restricted.template.json";

    public static string GetSafeTemplatePath(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        return Path.Combine(repositoryRoot, SafeTemplateRelativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    public static string GetRestrictedTemplatePath(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        return Path.Combine(repositoryRoot, RestrictedTemplateRelativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}

public static class OnlineTestEnvironmentVariables
{
    public const string SafeAccountBduss = "TIEBA_ONLINE_SAFE__ACCOUNT__BDUSS";
    public const string SafeAccountStoken = "TIEBA_ONLINE_SAFE__ACCOUNT__STOKEN";
    public const string SafeAssetsForumQuery = "TIEBA_ONLINE_SAFE__ASSETS__FORUMQUERY";
    public const string SafeAssetsForumName = "TIEBA_ONLINE_SAFE__ASSETS__FORUMNAME";
    public const string SafeAssetsForumId = "TIEBA_ONLINE_SAFE__ASSETS__FORUMID";
    public const string SafeAssetsManagerOwnedForumName = "TIEBA_ONLINE_SAFE__ASSETS__MANAGEROWNEDFORUMNAME";
    public const string SafeAssetsForumImageUrl = "TIEBA_ONLINE_SAFE__ASSETS__FORUMIMAGEURL";
    public const string SafeAssetsForumImageHash = "TIEBA_ONLINE_SAFE__ASSETS__FORUMIMAGEHASH";
    public const string SafeAssetsOwnedThreadId = "TIEBA_ONLINE_SAFE__ASSETS__OWNEDTHREADID";
    public const string SafeAssetsOwnedRootPostId = "TIEBA_ONLINE_SAFE__ASSETS__OWNEDROOTPOSTID";
    public const string SafeAssetsOwnedReplyId = "TIEBA_ONLINE_SAFE__ASSETS__OWNEDREPLYID";
    public const string SafeAssetsTargetUserName = "TIEBA_ONLINE_SAFE__ASSETS__TARGETUSERNAME";
    public const string SafeAssetsTargetUserId = "TIEBA_ONLINE_SAFE__ASSETS__TARGETUSERID";
    public const string SafeAssetsTargetTiebaUid = "TIEBA_ONLINE_SAFE__ASSETS__TARGETTIEBAUID";
    public const string SafeAssetsTargetPortrait = "TIEBA_ONLINE_SAFE__ASSETS__TARGETPORTRAIT";
    public const string SafeAssetsMessageRecipient = "TIEBA_ONLINE_SAFE__ASSETS__MESSAGERECIPIENT";
    public const string SafeAssetsChatroomId = "TIEBA_ONLINE_SAFE__ASSETS__CHATROOMID";

    public const string RestrictedOptIn = "TIEBA_ONLINE_RESTRICTED__OPTIN";
    public const string RestrictedAccountBduss = "TIEBA_ONLINE_RESTRICTED__ACCOUNT__BDUSS";
    public const string RestrictedAccountStoken = "TIEBA_ONLINE_RESTRICTED__ACCOUNT__STOKEN";
    public const string RestrictedCapabilitiesModeration = "TIEBA_ONLINE_RESTRICTED__CAPABILITIES__MODERATION";
    public const string RestrictedCapabilitiesAdmin = "TIEBA_ONLINE_RESTRICTED__CAPABILITIES__ADMIN";
    public const string RestrictedAssetsModerationForumName = "TIEBA_ONLINE_RESTRICTED__ASSETS__MODERATIONFORUMNAME";
    public const string RestrictedAssetsModerationForumId = "TIEBA_ONLINE_RESTRICTED__ASSETS__MODERATIONFORUMID";
    public const string RestrictedAssetsModerationThreadId = "TIEBA_ONLINE_RESTRICTED__ASSETS__MODERATIONTHREADID";
    public const string RestrictedAssetsModerationReplyId = "TIEBA_ONLINE_RESTRICTED__ASSETS__MODERATIONREPLYID";
    public const string RestrictedAssetsAdminUserName = "TIEBA_ONLINE_RESTRICTED__ASSETS__ADMINUSERNAME";
    public const string RestrictedAssetsAdminUserId = "TIEBA_ONLINE_RESTRICTED__ASSETS__ADMINUSERID";
    public const string RestrictedAssetsAdminPortrait = "TIEBA_ONLINE_RESTRICTED__ASSETS__ADMINPORTRAIT";
}
