#nullable enable
using System.Threading.Tasks;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Tests.Infrastructure.Configuration;
using AioTieba4DotNet.Tests.Infrastructure.Contracts;
using AioTieba4DotNet.Tests.Infrastructure.Execution;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Online.Safe.Features.ForumExtensions.Scenarios;

[TestClass]
[TestCategory(OnlineTestSuiteCategories.SafeOrdered)]
[TestCategory(OnlineTestFeatureCategories.ForumExtensions)]
[TestCategory(OnlineTestTierCategories.Safe)]
[TestCategory(OnlineTestStageCategories.ForumExtensions)]
[TestSubject(typeof(TiebaClient))]
public sealed class ForumExtensionsScenarioTests : OnlineSafeExecutionTestBase
{
    private const int SearchProbeLength = 2;
    private const int FollowScanPageSize = 200;
    private const int FollowScanMaxPages = 5;

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsSearchExactAsync)]
    public Task SearchExactAsync_StableForumId_ReturnsSearchPageShape()
    {
        return ExecuteSafeAsync(
            "forum extensions exact-search by-fid sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var context = await ResolveDedicatedForumContextAsync(
                    scope,
                    client,
                    nameof(SearchExactAsync_StableForumId_ReturnsSearchPageShape));
                var searches = await client.Forums.SearchExactAsync(context.ForumId, CreateSearchProbeQuery(context), 1, 20);

                Assert.IsNotNull(searches);
                Assert.IsNotNull(searches.Page);
                Assert.IsGreaterThanOrEqualTo(0, searches.Page.TotalCount);
                Assert.IsGreaterThanOrEqualTo(0, searches.Page.TotalPage);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsSearchExactAsync)]
    public Task SearchExactAsync_StableForum_ReturnsSearchPageShape()
    {
        return ExecuteSafeAsync(
            "forum extensions exact-search sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var context = await ResolveDedicatedForumContextAsync(
                    scope,
                    client,
                    nameof(SearchExactAsync_StableForum_ReturnsSearchPageShape));
                var searches = await client.Forums.SearchExactAsync(context.ForumName, CreateSearchProbeQuery(context), 1, 20);

                Assert.IsNotNull(searches);
                Assert.IsNotNull(searches.Page);
                Assert.IsGreaterThanOrEqualTo(0, searches.Page.TotalCount);
                Assert.IsGreaterThanOrEqualTo(0, searches.Page.TotalPage);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetLastReplyersAsync)]
    public Task GetLastReplyersAsync_StableForum_ReturnsThreadPageShape()
    {
        return ExecuteSafeAsync(
            "forum extensions last-replyers sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var context = await ResolveDedicatedForumContextAsync(
                    scope,
                    client,
                    nameof(GetLastReplyersAsync_StableForum_ReturnsThreadPageShape));
                var threads = await client.Forums.GetLastReplyersAsync(context.ForumName, 1, 20);

                Assert.IsNotNull(threads);
                Assert.IsNotNull(threads.Page);
                Assert.IsNotNull(threads.Forum);
                Assert.AreEqual(context.ForumName, threads.Forum.Fname);
                Assert.IsPositive(threads.Forum.Fid);
                Assert.IsGreaterThanOrEqualTo(0, threads.Page.TotalCount);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetRankForumsAsync)]
    public Task GetRankForumsAsync_StableForum_ReturnsRankPageShape()
    {
        return ExecuteSafeAsync(
            "forum extensions rank read",
            async scope =>
            {
                using var client = CreateClient(scope);
                var context = await ResolveDedicatedForumContextAsync(
                    scope,
                    client,
                    nameof(GetRankForumsAsync_StableForum_ReturnsRankPageShape));
                var ranks = await client.Forums.GetRankForumsAsync(context.ForumName, 1, ForumRankType.Weekly);

                Assert.IsNotNull(ranks);
                Assert.IsGreaterThanOrEqualTo(1, ranks.Page.CurrentPage);
                Assert.IsGreaterThanOrEqualTo(1, ranks.Page.TotalPage);
                if (ranks.Count > 0)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(ranks[0].Fname));
                    Assert.IsGreaterThanOrEqualTo(0, ranks[0].MemberNum);
                }
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetSelfFollowForumsAsync)]
    [TestCategory(OnlineTestApiCategories.ForumsFollowAsync)]
    [TestCategory(OnlineTestApiCategories.ForumsUnfollowAsync)]
    public Task FollowAndUnfollowAsync_DedicatedForum_UsesDisposableAssetCompensationAudit()
    {
        return ExecuteSafeAsync(
            "forum extensions dedicated follow lifecycle",
            async scope =>
            {
                using var client = CreateClient(scope);
                var context = await ResolveDedicatedForumContextAsync(
                    scope,
                    client,
                    nameof(FollowAndUnfollowAsync_DedicatedForum_UsesDisposableAssetCompensationAudit));
                var wasFollowed = await IsForumFollowedAsync(
                    client,
                    context,
                    nameof(FollowAndUnfollowAsync_DedicatedForum_UsesDisposableAssetCompensationAudit));
                if (wasFollowed)
                {
                    Assert.Inconclusive(
                        $"Skipping {nameof(FollowAndUnfollowAsync_DedicatedForum_UsesDisposableAssetCompensationAudit)}: dedicated forum '{context.ForumName}' is already followed by the safe account. Use a disposable forum asset that starts unfollowed so the scenario can prove a fresh follow/unfollow compensation cycle.");
                }

                var followSucceeded = await client.Forums.FollowAsync(context.ForumId);
                Assert.IsTrue(followSucceeded,
                    $"Expected the dedicated forum '{context.ForumName}' to accept a temporary safe follow mutation.");
                Assert.IsTrue(
                    await IsForumFollowedAsync(
                        client,
                        context,
                        nameof(FollowAndUnfollowAsync_DedicatedForum_UsesDisposableAssetCompensationAudit)),
                    "Expected the dedicated forum to appear in the authenticated self-follow listing after the safe follow mutation.");

                var followedArtifact = scope.Compensation.RecordMutatedArtifact(
                    OnlineTestStageCategories.ForumExtensions,
                    "forum-follow",
                    context.ForumId.ToString(CultureInfo.InvariantCulture),
                    $"temporary follow of dedicated forum '{context.ForumName}'");
                scope.Compensation.Register(
                    followedArtifact,
                    "undo dedicated forum follow",
                    "forum unfollowed",
                    cancellationToken => UnfollowAsync(client, context, cancellationToken));

                await scope.Compensation.ExecuteAsync();

                var audit = scope.Compensation.GetLastAudit();
                Assert.IsNotNull(audit);
                Assert.IsTrue(audit.Succeeded,
                    "Expected the ForumExtensions safe scenario to reconcile the dedicated forum follow mutation.");
                Assert.HasCount(1, audit.RecordedArtifacts);
                Assert.HasCount(1, audit.CompensationResults);
                Assert.IsEmpty(audit.UnreconciledArtifacts);
                Assert.AreEqual("forum unfollowed", audit.CompensationResults[0].CompensationOutcome);

                var auditDisplay = string.Join(global::System.Environment.NewLine, audit.ToDisplayLines());
                Assert.Contains(context.ForumName, auditDisplay);
                Assert.Contains("unreconciled: none", auditDisplay);
                Assert.IsFalse(
                    await IsForumFollowedAsync(
                        client,
                        context,
                        nameof(FollowAndUnfollowAsync_DedicatedForum_UsesDisposableAssetCompensationAudit)),
                    "Expected the dedicated forum follow mutation to be undone once compensation completed.");
            },
            OnlineExecutionCapability.Authenticated);
    }

    private static TiebaClient CreateClient(OnlineExecutionScope scope)
    {
        var options = new TiebaOptions
        {
            Bduss = scope.Safe.Account.IsConfigured ? scope.Safe.Account.Bduss : null,
            Stoken = scope.Safe.Account.IsConfigured ? scope.Safe.Account.Stoken : null,
            TransportMode = TiebaTransportMode.Http
        };

        return new TiebaClient(options);
    }

    private static async Task<DedicatedForumContext> ResolveDedicatedForumContextAsync(
        OnlineExecutionScope scope,
        TiebaClient client,
        string operationName)
    {
        var forumSelector = !string.IsNullOrWhiteSpace(scope.Safe.Assets.ForumQuery)
            ? scope.Safe.Assets.ForumQuery
            : scope.Safe.Assets.ForumName;

        if (string.IsNullOrWhiteSpace(forumSelector))
        {
            Assert.Inconclusive(
                $"Skipping {operationName}: forum-extension coverage requires an explicit dedicated safe forum asset. Set {OnlineTestEnvironmentVariables.SafeAssetsForumQuery} or {OnlineTestEnvironmentVariables.SafeAssetsForumName} instead of relying on a public fallback.");
        }

        var forum = await client.Forums.GetForumAsync(forumSelector);
        Assert.IsNotNull(forum);
        Assert.IsPositive(forum.Fid);
        Assert.IsFalse(string.IsNullOrWhiteSpace(forum.Fname));
        return new DedicatedForumContext(forumSelector, forum.Fname, checked((ulong)forum.Fid));
    }

    private static string CreateSearchProbeQuery(DedicatedForumContext context)
    {
        var source = !string.IsNullOrWhiteSpace(context.ForumName)
            ? context.ForumName
            : context.ForumSelector;
        return source.Length > SearchProbeLength
            ? source[..SearchProbeLength]
            : source;
    }

    private static async Task<bool> IsForumFollowedAsync(
        TiebaClient client,
        DedicatedForumContext context,
        string operationName)
    {
        for (var page = 1; page <= FollowScanMaxPages; page++)
        {
            var follows = await client.Forums.GetSelfFollowForumsAsync(page, FollowScanPageSize);
            if (follows.Any(forum => forum.Fid == context.ForumId
                                     || string.Equals(forum.Fname, context.ForumName, StringComparison.Ordinal)))
            {
                return true;
            }

            if (!follows.HasMore)
                return false;
        }

        Assert.Inconclusive(
            $"Skipping {operationName}: the bounded self-follow scan could not conclusively determine whether dedicated forum '{context.ForumName}' is already followed. Reduce the safe account's follow list or choose a clearer disposable forum asset.");
        return false;
    }

    private static async ValueTask UnfollowAsync(
        TiebaClient client,
        DedicatedForumContext context,
        CancellationToken cancellationToken)
    {
        var unfollowed = await client.Forums.UnfollowAsync(context.ForumId, cancellationToken);
        if (!unfollowed)
        {
            throw new InvalidOperationException(
                $"Expected to undo the temporary safe follow mutation for dedicated forum '{context.ForumName}'.");
        }
    }

    private sealed record DedicatedForumContext(string ForumSelector, string ForumName, ulong ForumId);
}
