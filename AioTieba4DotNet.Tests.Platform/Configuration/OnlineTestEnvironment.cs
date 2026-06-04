#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AioTieba4DotNet.Tests.Platform.Support;

namespace AioTieba4DotNet.Tests.Platform.Configuration;

public sealed class OnlineTestEnvironment
{
    public OnlineTestEnvironment(OnlineSafeProfile safe, OnlineRestrictedProfile restricted)
    {
        Safe = safe;
        Restricted = restricted;
    }

    public OnlineSafeProfile Safe { get; }

    public OnlineRestrictedProfile Restricted { get; }

    public static OnlineTestEnvironment LoadCurrent()
    {
        return LoadFromRepository(RepositoryPaths.FindRepositoryRoot());
    }

    public static OnlineTestEnvironment LoadFromRepository(string repositoryRoot,
        IReadOnlyDictionary<string, string?>? environmentVariables = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        using var safeDocument = JsonDocument.Parse(File.ReadAllText(OnlineTestEnvironmentFiles.GetSafeTemplatePath(repositoryRoot)));
        using var restrictedDocument = JsonDocument.Parse(File.ReadAllText(OnlineTestEnvironmentFiles.GetRestrictedTemplatePath(repositoryRoot)));

        var safeRoot = safeDocument.RootElement.GetProperty("safe");
        var restrictedRoot = restrictedDocument.RootElement.GetProperty("restricted");

        return new OnlineTestEnvironment(
            new OnlineSafeProfile(
                new OnlineTestAccount(
                    ResolveString(safeRoot, environmentVariables, OnlineTestEnvironmentVariables.SafeAccountBduss, "account", "bduss"),
                    ResolveString(safeRoot, environmentVariables, OnlineTestEnvironmentVariables.SafeAccountStoken, "account", "stoken")),
                new OnlineSafeAssets(
                    ResolveString(safeRoot, environmentVariables, OnlineTestEnvironmentVariables.SafeAssetsForumQuery, "assets", "forumQuery"),
                    ResolveString(safeRoot, environmentVariables, OnlineTestEnvironmentVariables.SafeAssetsForumName, "assets", "forumName"),
                    ResolveOptionalLong(safeRoot, environmentVariables, OnlineTestEnvironmentVariables.SafeAssetsForumId, "assets", "forumId"),
                    ResolveString(safeRoot, environmentVariables, OnlineTestEnvironmentVariables.SafeAssetsManagerOwnedForumName, "assets", "managerOwnedForumName"),
                    ResolveString(safeRoot, environmentVariables, OnlineTestEnvironmentVariables.SafeAssetsForumImageUrl, "assets", "forumImageUrl"),
                    ResolveString(safeRoot, environmentVariables, OnlineTestEnvironmentVariables.SafeAssetsForumImageHash, "assets", "forumImageHash"),
                    ResolveOptionalLong(safeRoot, environmentVariables, OnlineTestEnvironmentVariables.SafeAssetsOwnedThreadId, "assets", "ownedThreadId"),
                    ResolveOptionalLong(safeRoot, environmentVariables, OnlineTestEnvironmentVariables.SafeAssetsOwnedRootPostId, "assets", "ownedRootPostId"),
                    ResolveOptionalLong(safeRoot, environmentVariables, OnlineTestEnvironmentVariables.SafeAssetsOwnedReplyId, "assets", "ownedReplyId"),
                    ResolveString(safeRoot, environmentVariables, OnlineTestEnvironmentVariables.SafeAssetsTargetUserName, "assets", "targetUserName"),
                    ResolveOptionalLong(safeRoot, environmentVariables, OnlineTestEnvironmentVariables.SafeAssetsTargetUserId, "assets", "targetUserId"),
                    ResolveOptionalLong(safeRoot, environmentVariables, OnlineTestEnvironmentVariables.SafeAssetsTargetTiebaUid, "assets", "targetTiebaUid"),
                    ResolveString(safeRoot, environmentVariables, OnlineTestEnvironmentVariables.SafeAssetsTargetPortrait, "assets", "targetPortrait"),
                    ResolveString(safeRoot, environmentVariables, OnlineTestEnvironmentVariables.SafeAssetsMessageRecipient, "assets", "messageRecipient"),
                    ResolveOptionalLong(safeRoot, environmentVariables, OnlineTestEnvironmentVariables.SafeAssetsChatroomId, "assets", "chatroomId"))),
            new OnlineRestrictedProfile(
                ResolveBoolean(restrictedRoot, environmentVariables, OnlineTestEnvironmentVariables.RestrictedOptIn, "optIn"),
                new OnlineTestAccount(
                    ResolveString(restrictedRoot, environmentVariables, OnlineTestEnvironmentVariables.RestrictedAccountBduss, "account", "bduss"),
                    ResolveString(restrictedRoot, environmentVariables, OnlineTestEnvironmentVariables.RestrictedAccountStoken, "account", "stoken")),
                new OnlineRestrictedCapabilities(
                    ResolveBoolean(restrictedRoot, environmentVariables, OnlineTestEnvironmentVariables.RestrictedCapabilitiesModeration, "capabilities", "moderation"),
                    ResolveBoolean(restrictedRoot, environmentVariables, OnlineTestEnvironmentVariables.RestrictedCapabilitiesAdmin, "capabilities", "admin")),
                new OnlineRestrictedAssets(
                    ResolveString(restrictedRoot, environmentVariables, OnlineTestEnvironmentVariables.RestrictedAssetsModerationForumName, "assets", "moderationForumName"),
                    ResolveOptionalLong(restrictedRoot, environmentVariables, OnlineTestEnvironmentVariables.RestrictedAssetsModerationForumId, "assets", "moderationForumId"),
                    ResolveOptionalLong(restrictedRoot, environmentVariables, OnlineTestEnvironmentVariables.RestrictedAssetsModerationThreadId, "assets", "moderationThreadId"),
                    ResolveOptionalLong(restrictedRoot, environmentVariables, OnlineTestEnvironmentVariables.RestrictedAssetsModerationReplyId, "assets", "moderationReplyId"),
                    ResolveString(restrictedRoot, environmentVariables, OnlineTestEnvironmentVariables.RestrictedAssetsAdminUserName, "assets", "adminUserName"),
                    ResolveOptionalLong(restrictedRoot, environmentVariables, OnlineTestEnvironmentVariables.RestrictedAssetsAdminUserId, "assets", "adminUserId"),
                    ResolveString(restrictedRoot, environmentVariables, OnlineTestEnvironmentVariables.RestrictedAssetsAdminPortrait, "assets", "adminPortrait"))));
    }

    private static string ResolveString(JsonElement root, IReadOnlyDictionary<string, string?>? environmentVariables,
        string environmentVariableName, params string[] propertyPath)
    {
        if (TryGetEnvironmentValue(environmentVariables, environmentVariableName, out var environmentValue))
            return environmentValue;

        return TryGetProperty(root, propertyPath, out var property)
            ? property.ValueKind switch
            {
                JsonValueKind.String => property.GetString()?.Trim() ?? string.Empty,
                JsonValueKind.Null => string.Empty,
                _ => throw new InvalidOperationException(
                    $"Configuration path '{string.Join(':', propertyPath)}' in tracked template must be a string.")
            }
            : string.Empty;
    }

    private static bool ResolveBoolean(JsonElement root, IReadOnlyDictionary<string, string?>? environmentVariables,
        string environmentVariableName, params string[] propertyPath)
    {
        if (TryGetEnvironmentValue(environmentVariables, environmentVariableName, out var environmentValue))
        {
            if (bool.TryParse(environmentValue, out var parsedEnvironmentValue))
                return parsedEnvironmentValue;

            throw new InvalidOperationException(
                $"Environment variable '{environmentVariableName}' must be either 'true' or 'false'.");
        }

        if (!TryGetProperty(root, propertyPath, out var property))
            return false;

        return property.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw new InvalidOperationException(
                $"Configuration path '{string.Join(':', propertyPath)}' in tracked template must be a boolean.")
        };
    }

    private static long? ResolveOptionalLong(JsonElement root, IReadOnlyDictionary<string, string?>? environmentVariables,
        string environmentVariableName, params string[] propertyPath)
    {
        if (TryGetEnvironmentValue(environmentVariables, environmentVariableName, out var environmentValue))
            return ParseOptionalLong(environmentValue, environmentVariableName);

        if (!TryGetProperty(root, propertyPath, out var property))
            return null;

        return property.ValueKind switch
        {
            JsonValueKind.Number => ParseOptionalLong(property.GetRawText(), string.Join(':', propertyPath)),
            JsonValueKind.String => ParseOptionalLong(property.GetString() ?? string.Empty, string.Join(':', propertyPath)),
            JsonValueKind.Null => null,
            _ => throw new InvalidOperationException(
                $"Configuration path '{string.Join(':', propertyPath)}' in tracked template must be a number or empty string.")
        };
    }

    private static long? ParseOptionalLong(string rawValue, string sourceName)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return null;

        if (long.TryParse(rawValue.Trim(), out var parsedValue))
            return parsedValue > 0 ? parsedValue : null;

        throw new InvalidOperationException($"Value '{rawValue}' from '{sourceName}' must be a positive integer when configured.");
    }

    private static bool TryGetEnvironmentValue(IReadOnlyDictionary<string, string?>? environmentVariables,
        string environmentVariableName, out string value)
    {
        if (environmentVariables is not null)
        {
            if (environmentVariables.TryGetValue(environmentVariableName, out var configuredValue))
            {
                value = configuredValue?.Trim() ?? string.Empty;
                return true;
            }

            value = string.Empty;
            return false;
        }

        var environmentValue = Environment.GetEnvironmentVariable(environmentVariableName);
        if (environmentValue is not null)
        {
            value = environmentValue.Trim();
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryGetProperty(JsonElement root, IReadOnlyList<string> propertyPath, out JsonElement value)
    {
        value = root;
        foreach (var propertyName in propertyPath)
        {
            if (!value.TryGetProperty(propertyName, out value))
                return false;
        }

        return true;
    }
}

public sealed record OnlineTestAccount(string Bduss, string Stoken)
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Bduss);
}

public sealed record OnlineSafeAssets(
    string ForumQuery,
    string ForumName,
    long? ForumId,
    string ManagerOwnedForumName,
    string ForumImageUrl,
    string ForumImageHash,
    long? OwnedThreadId,
    long? OwnedRootPostId,
    long? OwnedReplyId,
    string TargetUserName,
    long? TargetUserId,
    long? TargetTiebaUid,
    string TargetPortrait,
    string MessageRecipient,
    long? ChatroomId);

public sealed record OnlineRestrictedCapabilities(bool Moderation, bool Admin);

public sealed record OnlineRestrictedAssets(
    string ModerationForumName,
    long? ModerationForumId,
    long? ModerationThreadId,
    long? ModerationReplyId,
    string AdminUserName,
    long? AdminUserId,
    string AdminPortrait);

public sealed record OnlineSafeProfile(OnlineTestAccount Account, OnlineSafeAssets Assets);

public sealed record OnlineRestrictedProfile(
    bool OptIn,
    OnlineTestAccount Account,
    OnlineRestrictedCapabilities Capabilities,
    OnlineRestrictedAssets Assets);
