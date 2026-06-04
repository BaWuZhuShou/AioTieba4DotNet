#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AioTieba4DotNet.Tests.Platform.Configuration;
using AioTieba4DotNet.Tests.Platform.Contracts;
using AioTieba4DotNet.Tests.Platform.Execution;
using AioTieba4DotNet.Tests.Platform.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Governance.Contracts;

[TestClass]
[TestCategory(OnlineTestContractCategories.Environment)]
public sealed class OnlineEnvironmentContractTests
{
    [TestMethod]
    public void TrackedEnvironmentTemplatesKeepCredentialsAndLiveAssetsBlank()
    {
        using var safeDocument = JsonDocument.Parse(File.ReadAllText(OnlineTestEnvironmentFiles.GetSafeTemplatePath(RepositoryPaths.FindRepositoryRoot())));
        using var restrictedDocument = JsonDocument.Parse(File.ReadAllText(OnlineTestEnvironmentFiles.GetRestrictedTemplatePath(RepositoryPaths.FindRepositoryRoot())));

        AssertBlankString(safeDocument.RootElement, "safe", "account", "bduss");
        AssertBlankString(safeDocument.RootElement, "safe", "account", "stoken");
        AssertBlankString(safeDocument.RootElement, "safe", "assets", "forumQuery");
        AssertBlankString(safeDocument.RootElement, "safe", "assets", "forumName");
        AssertZeroOrBlankLong(safeDocument.RootElement, "safe", "assets", "forumId");
        AssertBlankString(safeDocument.RootElement, "safe", "assets", "managerOwnedForumName");
        AssertBlankString(safeDocument.RootElement, "safe", "assets", "forumImageUrl");
        AssertBlankString(safeDocument.RootElement, "safe", "assets", "forumImageHash");
        AssertZeroOrBlankLong(safeDocument.RootElement, "safe", "assets", "ownedThreadId");
        AssertZeroOrBlankLong(safeDocument.RootElement, "safe", "assets", "ownedRootPostId");
        AssertZeroOrBlankLong(safeDocument.RootElement, "safe", "assets", "ownedReplyId");
        AssertBlankString(safeDocument.RootElement, "safe", "assets", "targetUserName");
        AssertZeroOrBlankLong(safeDocument.RootElement, "safe", "assets", "targetUserId");
        AssertZeroOrBlankLong(safeDocument.RootElement, "safe", "assets", "targetTiebaUid");
        AssertBlankString(safeDocument.RootElement, "safe", "assets", "targetPortrait");
        AssertBlankString(safeDocument.RootElement, "safe", "assets", "messageRecipient");
        AssertZeroOrBlankLong(safeDocument.RootElement, "safe", "assets", "chatroomId");

        AssertBoolean(restrictedDocument.RootElement, false, "restricted", "optIn");
        AssertBlankString(restrictedDocument.RootElement, "restricted", "account", "bduss");
        AssertBlankString(restrictedDocument.RootElement, "restricted", "account", "stoken");
        AssertBoolean(restrictedDocument.RootElement, false, "restricted", "capabilities", "moderation");
        AssertBoolean(restrictedDocument.RootElement, false, "restricted", "capabilities", "admin");
        AssertBlankString(restrictedDocument.RootElement, "restricted", "assets", "moderationForumName");
        AssertZeroOrBlankLong(restrictedDocument.RootElement, "restricted", "assets", "moderationForumId");
        AssertZeroOrBlankLong(restrictedDocument.RootElement, "restricted", "assets", "moderationThreadId");
        AssertZeroOrBlankLong(restrictedDocument.RootElement, "restricted", "assets", "moderationReplyId");
        AssertBlankString(restrictedDocument.RootElement, "restricted", "assets", "adminUserName");
        AssertZeroOrBlankLong(restrictedDocument.RootElement, "restricted", "assets", "adminUserId");
        AssertBlankString(restrictedDocument.RootElement, "restricted", "assets", "adminPortrait");
    }

    [TestMethod]
    public void BlankTrackedTemplatesLoadAsExplicitlyUnconfiguredProfiles()
    {
        var environment = OnlineTestEnvironment.LoadFromRepository(
            RepositoryPaths.FindRepositoryRoot(),
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase));

        Assert.IsEmpty(environment.Safe.Account.Bduss);
        Assert.IsEmpty(environment.Safe.Account.Stoken);
        Assert.IsEmpty(environment.Safe.Assets.ForumQuery);
        Assert.IsEmpty(environment.Safe.Assets.ForumName);
        Assert.IsNull(environment.Safe.Assets.ForumId);
        Assert.IsEmpty(environment.Safe.Assets.ManagerOwnedForumName);
        Assert.IsEmpty(environment.Safe.Assets.ForumImageUrl);
        Assert.IsEmpty(environment.Safe.Assets.ForumImageHash);
        Assert.IsNull(environment.Safe.Assets.OwnedThreadId);
        Assert.IsNull(environment.Safe.Assets.OwnedRootPostId);
        Assert.IsNull(environment.Safe.Assets.OwnedReplyId);
        Assert.IsEmpty(environment.Safe.Assets.TargetUserName);
        Assert.IsNull(environment.Safe.Assets.TargetUserId);
        Assert.IsNull(environment.Safe.Assets.TargetTiebaUid);
        Assert.IsEmpty(environment.Safe.Assets.TargetPortrait);
        Assert.IsEmpty(environment.Safe.Assets.MessageRecipient);
        Assert.IsNull(environment.Safe.Assets.ChatroomId);

        Assert.IsFalse(environment.Restricted.OptIn);
        Assert.IsEmpty(environment.Restricted.Account.Bduss);
        Assert.IsEmpty(environment.Restricted.Account.Stoken);
        Assert.IsFalse(environment.Restricted.Capabilities.Moderation);
        Assert.IsFalse(environment.Restricted.Capabilities.Admin);
        Assert.IsEmpty(environment.Restricted.Assets.ModerationForumName);
        Assert.IsNull(environment.Restricted.Assets.ModerationForumId);
        Assert.IsNull(environment.Restricted.Assets.ModerationThreadId);
        Assert.IsNull(environment.Restricted.Assets.ModerationReplyId);
        Assert.IsEmpty(environment.Restricted.Assets.AdminUserName);
        Assert.IsNull(environment.Restricted.Assets.AdminUserId);
        Assert.IsEmpty(environment.Restricted.Assets.AdminPortrait);
    }

    [TestMethod]
    public void RestrictedModerationContractRequiresExplicitOptIn()
    {
        var environment = OnlineTestEnvironment.LoadFromRepository(
            RepositoryPaths.FindRepositoryRoot(),
            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase));

        var exception = Assert.ThrowsExactly<AssertInconclusiveException>(
            () => OnlineExecutionGate.RequireRestricted(environment, "restricted moderation contract probe", OnlineExecutionCapability.Moderation));

        Assert.Contains("explicit opt-in", exception.Message);
        Assert.Contains(OnlineTestEnvironmentVariables.RestrictedOptIn, exception.Message);
    }

    [TestMethod]
    public void RestrictedModerationContractRequiresDedicatedRestrictedCredentialsAfterOptIn()
    {
        var environment = CreateEnvironment(
            (OnlineTestEnvironmentVariables.RestrictedOptIn, "true"),
            (OnlineTestEnvironmentVariables.RestrictedCapabilitiesModeration, "true"));

        var exception = Assert.ThrowsExactly<AssertInconclusiveException>(
            () => OnlineExecutionGate.RequireRestricted(environment, "restricted moderation contract probe", OnlineExecutionCapability.Moderation));

        Assert.Contains("dedicated restricted credentials", exception.Message);
        Assert.Contains(OnlineTestEnvironmentVariables.RestrictedAccountBduss, exception.Message);
    }

    [TestMethod]
    public void RestrictedModerationContractRequiresCapabilityAfterOptInAndCredentials()
    {
        var environment = CreateEnvironment(
            (OnlineTestEnvironmentVariables.RestrictedOptIn, "true"),
            (OnlineTestEnvironmentVariables.RestrictedAccountBduss, "restricted-bduss"));

        var exception = Assert.ThrowsExactly<AssertInconclusiveException>(
            () => OnlineExecutionGate.RequireRestricted(environment, "restricted moderation contract probe", OnlineExecutionCapability.Moderation));

        Assert.Contains("explicit capability opt-in", exception.Message);
        Assert.Contains(OnlineTestEnvironmentVariables.RestrictedCapabilitiesModeration, exception.Message);
    }

    [TestMethod]
    public void RestrictedAdminContractRequiresAdminCapabilityAfterOptInAndCredentials()
    {
        var environment = CreateEnvironment(
            (OnlineTestEnvironmentVariables.RestrictedOptIn, "true"),
            (OnlineTestEnvironmentVariables.RestrictedAccountBduss, "restricted-bduss"));

        var exception = Assert.ThrowsExactly<AssertInconclusiveException>(
            () => OnlineExecutionGate.RequireRestricted(environment, "restricted admin contract probe", OnlineExecutionCapability.Admin));

        Assert.Contains("explicit capability opt-in", exception.Message);
        Assert.Contains(OnlineTestEnvironmentVariables.RestrictedCapabilitiesAdmin, exception.Message);
    }

    private static OnlineTestEnvironment CreateEnvironment(params (string Key, string? Value)[] overrides)
    {
        var environmentVariables = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in overrides)
            environmentVariables[key] = value;

        return OnlineTestEnvironment.LoadFromRepository(RepositoryPaths.FindRepositoryRoot(), environmentVariables);
    }

    private static void AssertBlankString(JsonElement root, params string[] propertyPath)
    {
        var property = GetProperty(root, propertyPath);
        Assert.AreEqual(JsonValueKind.String, property.ValueKind, $"Expected {string.Join(':', propertyPath)} to be a string.");
        Assert.IsTrue(string.IsNullOrWhiteSpace(property.GetString()),
            $"Tracked template field {string.Join(':', propertyPath)} must stay blank.");
    }

    private static void AssertZeroOrBlankLong(JsonElement root, params string[] propertyPath)
    {
        var property = GetProperty(root, propertyPath);
        switch (property.ValueKind)
        {
            case JsonValueKind.Number:
                Assert.AreEqual(0L, property.GetInt64(), $"Tracked template field {string.Join(':', propertyPath)} must stay zero.");
                break;
            case JsonValueKind.String:
                Assert.IsTrue(string.IsNullOrWhiteSpace(property.GetString()),
                    $"Tracked template field {string.Join(':', propertyPath)} must stay blank.");
                break;
            default:
                Assert.Fail($"Tracked template field {string.Join(':', propertyPath)} must be a number or blank string.");
                break;
        }
    }

    private static void AssertBoolean(JsonElement root, bool expectedValue, params string[] propertyPath)
    {
        var property = GetProperty(root, propertyPath);
        Assert.IsTrue(
            property.ValueKind is JsonValueKind.True or JsonValueKind.False,
            $"Expected {string.Join(':', propertyPath)} to be a boolean.");
        Assert.AreEqual(expectedValue, property.GetBoolean(),
            $"Tracked template field {string.Join(':', propertyPath)} must stay {expectedValue.ToString().ToLowerInvariant()}.");
    }

    private static JsonElement GetProperty(JsonElement root, params string[] propertyPath)
    {
        var current = root;
        foreach (var propertyName in propertyPath)
            current = current.GetProperty(propertyName);

        return current;
    }
}
