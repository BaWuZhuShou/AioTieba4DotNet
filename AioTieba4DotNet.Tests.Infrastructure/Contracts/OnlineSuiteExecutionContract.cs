#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace AioTieba4DotNet.Tests.Infrastructure.Contracts;

[ExcludeFromCodeCoverage]
public static class OnlineSuiteExecutionContract
{
    private static readonly OnlineOrderedSuiteStage[] OrderedStageMatrix =
    [
        new(
            1,
            OnlineTestProjectTopology.Safe,
            OnlineTestSuiteCategories.SafeOrdered,
            OnlineTestTierCategories.Safe,
            OnlineTestFeatureCategories.ForumFoundation,
            OnlineTestStageCategories.ForumFoundation,
            null,
            "Bootstraps safe forum identifiers and forum detail reads before any later ordered suite stage depends on them."),
        new(
            2,
            OnlineTestProjectTopology.Safe,
            OnlineTestSuiteCategories.SafeOrdered,
            OnlineTestTierCategories.Safe,
            OnlineTestFeatureCategories.ForumExtensions,
            OnlineTestStageCategories.ForumExtensions,
            OnlineTestCapabilityCategories.Authenticated,
            "Runs authenticated forum-extension coverage only after the forum foundation stage has established safe forum prerequisites."),
        new(
            3,
            OnlineTestProjectTopology.Safe,
            OnlineTestSuiteCategories.SafeOrdered,
            OnlineTestTierCategories.Safe,
            OnlineTestFeatureCategories.ThreadRead,
            OnlineTestStageCategories.ThreadRead,
            null,
            "Executes thread, post, and comment reads before later user, messaging, or write stages rely on discovered objects."),
        new(
            4,
            OnlineTestProjectTopology.Safe,
            OnlineTestSuiteCategories.SafeOrdered,
            OnlineTestTierCategories.Safe,
            OnlineTestFeatureCategories.UserSocial,
            OnlineTestStageCategories.UserSocial,
            OnlineTestCapabilityCategories.Authenticated,
            "Runs authenticated user and social reads after forum and thread prerequisites are already known-good."),
        new(
            5,
            OnlineTestProjectTopology.Safe,
            OnlineTestSuiteCategories.SafeOrdered,
            OnlineTestTierCategories.Safe,
            OnlineTestFeatureCategories.Messaging,
            OnlineTestStageCategories.Messaging,
            OnlineTestCapabilityCategories.Messaging,
            "Runs messaging and chatroom coverage after forum and user prerequisites are established, while preserving endpoint-specific explicit gating."),
        new(
            6,
            OnlineTestProjectTopology.Safe,
            OnlineTestSuiteCategories.SafeOrdered,
            OnlineTestTierCategories.Safe,
            OnlineTestFeatureCategories.ThreadWrite,
            OnlineTestStageCategories.ThreadWrite,
            OnlineTestCapabilityCategories.Authenticated,
            "Runs reversible safe thread-write coverage last so the suite owns ordered mutation execution and compensation-audit visibility."),
        new(
            7,
            OnlineTestProjectTopology.Restricted,
            OnlineTestSuiteCategories.RestrictedOrdered,
            OnlineTestTierCategories.Restricted,
            OnlineTestFeatureCategories.Moderation,
            OnlineTestStageCategories.ModerationRestricted,
            OnlineTestCapabilityCategories.Moderation,
            "Runs restricted moderation coverage only when the restricted suite is explicitly selected with dedicated restricted credentials and capability opt-in."),
        new(
            8,
            OnlineTestProjectTopology.Restricted,
            OnlineTestSuiteCategories.RestrictedOrdered,
            OnlineTestTierCategories.Restricted,
            OnlineTestFeatureCategories.Admin,
            OnlineTestStageCategories.AdminRestricted,
            OnlineTestCapabilityCategories.Admin,
            "Runs restricted admin coverage only after explicit restricted selection, isolated from the default safe ordered path.")
    ];

    public static readonly OnlineFeatureMetadata[] FeatureMatrix = OrderedStageMatrix
        .Select(static stage => new OnlineFeatureMetadata(
            stage.FeatureCategory,
            stage.TierCategory,
            stage.StageCategory,
            stage.CapabilityCategory))
        .ToArray();

    public static readonly OnlineOrderedSuiteDefinition SafeOrderedSuite = CreateOrderedSuite(
        OnlineTestSuiteCategories.SafeOrdered,
        OnlineTestTierCategories.Safe,
        requiresExplicitSelection: false);

    public static readonly OnlineOrderedSuiteDefinition RestrictedOrderedSuite = CreateOrderedSuite(
        OnlineTestSuiteCategories.RestrictedOrdered,
        OnlineTestTierCategories.Restricted,
        requiresExplicitSelection: true);

    public static readonly string[] DefaultTierCategories =
    [
        OnlineTestTierCategories.Safe
    ];

    public static readonly string[] DefaultSuiteCategories =
    [
        OnlineTestSuiteCategories.SafeOrdered
    ];

    public static readonly string[] SafeOrderedStageCategories = SafeOrderedSuite.OrderedStageCategories.ToArray();

    public static readonly string[] RestrictedOrderedStageCategories = RestrictedOrderedSuite.OrderedStageCategories.ToArray();

    public static readonly string[] RestrictedOptInCategories =
    [
        OnlineTestTierCategories.Restricted,
        OnlineTestSuiteCategories.RestrictedOrdered
    ];

    public const string CompensationAudit = "CompensationAudit";

    public static OnlineOrderedSuiteDefinition GetOrderedSuite(string suiteCategory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suiteCategory);

        return suiteCategory switch
        {
            OnlineTestSuiteCategories.SafeOrdered => SafeOrderedSuite,
            OnlineTestSuiteCategories.RestrictedOrdered => RestrictedOrderedSuite,
            _ => throw new ArgumentOutOfRangeException(nameof(suiteCategory), suiteCategory,
                $"Unknown ordered suite category '{suiteCategory}'.")
        };
    }

    private static OnlineOrderedSuiteDefinition CreateOrderedSuite(
        string suiteCategory,
        string tierCategory,
        bool requiresExplicitSelection)
    {
        var stages = OrderedStageMatrix
            .Where(stage => string.Equals(stage.SuiteCategory, suiteCategory, StringComparison.Ordinal))
            .OrderBy(static stage => stage.Order)
            .ToArray();

        return new OnlineOrderedSuiteDefinition(suiteCategory, tierCategory, requiresExplicitSelection, stages);
    }
}
