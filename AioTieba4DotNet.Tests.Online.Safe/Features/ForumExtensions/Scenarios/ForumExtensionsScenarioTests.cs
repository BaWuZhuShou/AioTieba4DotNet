#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Contents;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Models.Threads;
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
    private const int DislikeScanPageSize = 20;
    private const int DislikeScanMaxPages = 5;
    private const int ThreadDiscoveryPageSize = 10;
    private const int ThreadDiscoveryPages = 2;
    private const int ThreadCandidateCount = 5;
    private const int PostDiscoveryPageSize = 20;

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsSearchExactAsync)]
    public Task SearchExactAsyncStableForumIdReturnsSearchPageShape()
    {
        return ExecuteSafeAsync(
            "forum extensions exact-search by-fid sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var context = await ResolveDedicatedForumContextAsync(
                    scope,
                    client,
                    nameof(SearchExactAsyncStableForumIdReturnsSearchPageShape));
                var searches = await client.Forums.SearchExactAsync(context.ForumId, CreateSearchProbeQuery(context), 1, 20);

                Assert.IsNotNull(searches);
                Assert.IsNotNull(searches.Page);
                Assert.IsGreaterThanOrEqualTo(0, searches.Page.TotalCount);
                Assert.IsGreaterThanOrEqualTo(0, searches.Page.TotalPage);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsSearchExactAsync)]
    public Task SearchExactAsyncStableForumReturnsSearchPageShape()
    {
        return ExecuteSafeAsync(
            "forum extensions exact-search sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var context = await ResolveDedicatedForumContextAsync(
                    scope,
                    client,
                    nameof(SearchExactAsyncStableForumReturnsSearchPageShape));
                var searches = await client.Forums.SearchExactAsync(context.ForumName, CreateSearchProbeQuery(context), 1, 20);

                Assert.IsNotNull(searches);
                Assert.IsNotNull(searches.Page);
                Assert.IsGreaterThanOrEqualTo(0, searches.Page.TotalCount);
                Assert.IsGreaterThanOrEqualTo(0, searches.Page.TotalPage);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetLastReplyersAsync)]
    public Task GetLastReplyersAsyncStableForumReturnsThreadPageShapeForNameAndFidOverloads()
    {
        return ExecuteSafeAsync(
            "forum extensions last-replyers sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var context = await ResolveDedicatedForumContextAsync(
                    scope,
                    client,
                    nameof(GetLastReplyersAsyncStableForumReturnsThreadPageShapeForNameAndFidOverloads));
                var threadsByName = await client.Forums.GetLastReplyersAsync(context.ForumName, 1, 20);
                var threadsByFid = await client.Forums.GetLastReplyersAsync(context.ForumId, 1, 20);

                Assert.IsNotNull(threadsByName);
                Assert.IsNotNull(threadsByName.Page);
                Assert.IsNotNull(threadsByName.Forum);
                Assert.AreEqual(context.ForumName, threadsByName.Forum.Fname);
                Assert.IsPositive(threadsByName.Forum.Fid);
                Assert.IsGreaterThanOrEqualTo(0, threadsByName.Page.TotalCount);

                Assert.IsNotNull(threadsByFid);
                Assert.IsNotNull(threadsByFid.Page);
                Assert.IsNotNull(threadsByFid.Forum);
                Assert.AreEqual((long)context.ForumId, threadsByFid.Forum.Fid);
                Assert.AreEqual(context.ForumName, threadsByFid.Forum.Fname);
                Assert.IsGreaterThanOrEqualTo(0, threadsByFid.Page.TotalCount);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetRankForumsAsync)]
    public Task GetRankForumsAsyncStableForumReturnsRankPageShapeForNameAndFidOverloads()
    {
        return ExecuteSafeAsync(
            "forum extensions rank read",
            async scope =>
            {
                using var client = CreateClient(scope);
                var context = await ResolveDedicatedForumContextAsync(
                    scope,
                    client,
                    nameof(GetRankForumsAsyncStableForumReturnsRankPageShapeForNameAndFidOverloads));
                var ranksByName = await client.Forums.GetRankForumsAsync(context.ForumName, 1, ForumRankType.Weekly);
                var ranksByFid = await client.Forums.GetRankForumsAsync(context.ForumId, 1, ForumRankType.Weekly);

                AssertRankForumsShape(ranksByName);
                AssertRankForumsShape(ranksByFid);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetFollowForumsAsync)]
    [TestCategory(OnlineTestApiCategories.ForumsGetSelfFollowForumsV1Async)]
    [TestCategory(OnlineTestApiCategories.ForumsGetCidAsync)]
    [TestCategory(OnlineTestApiCategories.ForumsGetMemberUsersAsync)]
    [TestCategory(OnlineTestApiCategories.ForumsGetRecomStatusAsync)]
    [TestCategory(OnlineTestApiCategories.ForumsGetSquareForumsAsync)]
    [TestCategory(OnlineTestApiCategories.ForumsGetStatisticsAsync)]
    [TestCategory(OnlineTestApiCategories.ForumsGetForumLevelAsync)]
    [TestCategory(OnlineTestApiCategories.ForumsGetRoomListByFidAsync)]
    public Task ForumExtensionReadsDedicatedSafeAssetsReturnExpectedShapes()
    {
        return ExecuteSafeAsync(
            "forum extensions read matrix sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(ForumExtensionReadsDedicatedSafeAssetsReturnExpectedShapes);
                var context = await ResolveDedicatedForumContextAsync(scope, client, operationName);
                EnsureSafeStoken(scope, operationName);
                var targetUserId = RequireTargetUserId(scope, operationName);

                var followForums = await client.Forums.GetFollowForumsAsync(targetUserId, 1, 20);
                var selfFollowForumsV1 = await client.Forums.GetSelfFollowForumsV1Async(1, 20);
                var cidByName = await client.Forums.GetCidAsync(context.ForumName);
                var cidByFid = await client.Forums.GetCidAsync(context.ForumId);
                var memberUsersByName = await client.Forums.GetMemberUsersAsync(context.ForumName, 1);
                var memberUsersByFid = await client.Forums.GetMemberUsersAsync(context.ForumId, 1);
                var recomStatusByName = await client.Forums.GetRecomStatusAsync(context.ForumName);
                var recomStatusByFid = await client.Forums.GetRecomStatusAsync(context.ForumId);
                var squareForumsProbe = await QuerySquareForumsAsync(scope, client, context, operationName);
                var statisticsByName = await client.Forums.GetStatisticsAsync(context.ForumName);
                var statisticsByFid = await client.Forums.GetStatisticsAsync(context.ForumId);
                var forumLevelByName = await client.Forums.GetForumLevelAsync(context.ForumName);
                var forumLevelByFid = await client.Forums.GetForumLevelAsync(context.ForumId);
                var roomList = await client.Forums.GetRoomListByFidAsync(context.ForumId);

                Assert.IsNotNull(followForums);
                if (followForums.Count > 0)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(followForums[0].Fname));
                    Assert.IsPositive(followForums[0].Fid);
                }

                Assert.IsNotNull(selfFollowForumsV1);
                Assert.IsNotNull(selfFollowForumsV1.Page);
                Assert.IsGreaterThanOrEqualTo(1, selfFollowForumsV1.Page.CurrentPage);
                Assert.IsGreaterThanOrEqualTo(0, selfFollowForumsV1.Count);

                Assert.AreEqual(0, cidByName);
                Assert.AreEqual(0, cidByFid);

                AssertMemberUsersShape(memberUsersByName);
                AssertMemberUsersShape(memberUsersByFid);

                AssertRecomStatusShape(recomStatusByName);
                AssertRecomStatusShape(recomStatusByFid);

                Assert.IsNotNull(squareForumsProbe.Result);
                Assert.IsNotNull(squareForumsProbe.Result.Page);
                Assert.IsGreaterThanOrEqualTo(1, squareForumsProbe.Result.Page.CurrentPage);
                Assert.IsGreaterThanOrEqualTo(0, squareForumsProbe.Result.Count);

                AssertStatisticsShape(statisticsByName);
                AssertStatisticsShape(statisticsByFid);

                AssertForumLevelShape(forumLevelByName);
                AssertForumLevelShape(forumLevelByFid);

                Assert.IsNotNull(roomList);
                Assert.IsGreaterThanOrEqualTo(0, roomList.Count);
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetPortraitAsync)]
    [TestCategory(OnlineTestApiCategories.ForumsGetImageAsync)]
    [TestCategory(OnlineTestApiCategories.ForumsGetImageBytesAsync)]
    [TestCategory(OnlineTestApiCategories.ForumsGetImageByHashAsync)]
    public Task ForumMediaReadsDedicatedPortraitAndForumImagesReturnImagePayloads()
    {
        return ExecuteSafeAsync(
            "forum extensions media read sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(ForumMediaReadsDedicatedPortraitAndForumImagesReturnImagePayloads);
                var context = await ResolveDedicatedForumContextAsync(scope, client, operationName);
                var portrait = RequireTargetPortrait(scope, operationName);
                var portraitImage = await client.Forums.GetPortraitAsync(portrait, ForumImageSize.Small);
                var imageProbe = await RequireImageProbeAsync(client, context, operationName);
                var forumImage = await client.Forums.GetImageAsync(imageProbe.ImageUrl);
                var forumImageBytes = await client.Forums.GetImageBytesAsync(imageProbe.ImageUrl);
                var forumImageByHash = await client.Forums.GetImageByHashAsync(imageProbe.Hash, ForumImageSize.Small);

                AssertImageShape(portraitImage);
                AssertImageShape(forumImage);
                Assert.IsFalse(forumImageBytes.IsEmpty, "Expected the forum image byte probe to download raw bytes.");
                Assert.IsPositive(forumImageBytes.Data.Length);
                AssertImageShape(forumImageByHash);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetSelfFollowForumsAsync)]
    [TestCategory(OnlineTestApiCategories.ForumsFollowAsync)]
    [TestCategory(OnlineTestApiCategories.ForumsUnfollowAsync)]
    public Task FollowAndUnfollowAsyncDedicatedForumUsesBothOverloadsAndCompensationAudit()
    {
        return ExecuteSafeAsync(
            "forum extensions dedicated follow lifecycle",
            async scope =>
            {
                using var client = CreateClient(scope);
                var context = await ResolveDedicatedForumContextAsync(
                    scope,
                    client,
                    nameof(FollowAndUnfollowAsyncDedicatedForumUsesBothOverloadsAndCompensationAudit));
                var wasFollowed = await IsForumFollowedAsync(
                    client,
                    context,
                    nameof(FollowAndUnfollowAsyncDedicatedForumUsesBothOverloadsAndCompensationAudit));
                if (wasFollowed)
                {
                    Assert.Inconclusive(
                        $"Skipping {nameof(FollowAndUnfollowAsyncDedicatedForumUsesBothOverloadsAndCompensationAudit)}: dedicated forum '{context.ForumName}' is already followed by the safe account. Use a disposable forum asset that starts unfollowed so the scenario can prove both follow/unfollow overloads.");
                }

                var followedByName = await client.Forums.FollowAsync(context.ForumName);
                Assert.IsTrue(followedByName,
                    $"Expected the dedicated forum '{context.ForumName}' to accept a temporary safe follow mutation via the name overload.");
                Assert.IsTrue(
                    await IsForumFollowedAsync(
                        client,
                        context,
                        nameof(FollowAndUnfollowAsyncDedicatedForumUsesBothOverloadsAndCompensationAudit)),
                    "Expected the dedicated forum to appear in the authenticated self-follow listing after the name-based follow mutation.");

                var unfollowedByFid = await client.Forums.UnfollowAsync(context.ForumId);
                Assert.IsTrue(unfollowedByFid,
                    $"Expected the dedicated forum '{context.ForumName}' to accept a temporary safe unfollow mutation via the fid overload.");
                Assert.IsFalse(
                    await IsForumFollowedAsync(
                        client,
                        context,
                        nameof(FollowAndUnfollowAsyncDedicatedForumUsesBothOverloadsAndCompensationAudit)),
                    "Expected the dedicated forum to disappear from the authenticated self-follow listing after the fid-based unfollow mutation.");

                var followedByFid = await client.Forums.FollowAsync(context.ForumId);
                Assert.IsTrue(followedByFid,
                    $"Expected the dedicated forum '{context.ForumName}' to accept a temporary safe follow mutation via the fid overload.");
                Assert.IsTrue(
                    await IsForumFollowedAsync(
                        client,
                        context,
                        nameof(FollowAndUnfollowAsyncDedicatedForumUsesBothOverloadsAndCompensationAudit)),
                    "Expected the dedicated forum to reappear in the authenticated self-follow listing after the fid-based follow mutation.");

                var followedArtifact = scope.Compensation.RecordMutatedArtifact(
                    OnlineTestStageCategories.ForumExtensions,
                    "forum-follow",
                    context.ForumId.ToString(CultureInfo.InvariantCulture),
                    $"temporary follow of dedicated forum '{context.ForumName}' via the fid overload");
                scope.Compensation.Register(
                    followedArtifact,
                    "undo dedicated forum follow by name",
                    "forum unfollowed",
                    cancellationToken => UnfollowByNameAsync(client, context, cancellationToken));

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
                        nameof(FollowAndUnfollowAsyncDedicatedForumUsesBothOverloadsAndCompensationAudit)),
                    "Expected the dedicated forum follow mutation to be undone once compensation completed.");
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsDislikeAsync)]
    [TestCategory(OnlineTestApiCategories.ForumsUndislikeAsync)]
    [TestCategory(OnlineTestApiCategories.ForumsGetDislikeForumsAsync)]
    public Task DislikeAndUndislikeAsyncDedicatedForumUsesBothOverloadsAndCompensationAudit()
    {
        return ExecuteSafeAsync(
            "forum extensions dedicated dislike lifecycle",
            async scope =>
            {
                using var client = CreateClient(scope);
                var context = await ResolveDedicatedForumContextAsync(
                    scope,
                    client,
                    nameof(DislikeAndUndislikeAsyncDedicatedForumUsesBothOverloadsAndCompensationAudit));
                var wasDisliked = await IsForumDislikedAsync(
                    client,
                    context,
                    nameof(DislikeAndUndislikeAsyncDedicatedForumUsesBothOverloadsAndCompensationAudit));
                if (wasDisliked)
                {
                    Assert.Inconclusive(
                        $"Skipping {nameof(DislikeAndUndislikeAsyncDedicatedForumUsesBothOverloadsAndCompensationAudit)}: dedicated forum '{context.ForumName}' is already disliked by the safe account. Use a disposable forum asset that starts outside the homepage-dislike list so the scenario can prove both dislike/undislike overloads.");
                }

                var dislikedByName = await client.Forums.DislikeAsync(context.ForumName);
                Assert.IsTrue(dislikedByName,
                    $"Expected the dedicated forum '{context.ForumName}' to accept a temporary homepage-dislike mutation via the name overload.");
                Assert.IsTrue(
                    await IsForumDislikedAsync(
                        client,
                        context,
                        nameof(DislikeAndUndislikeAsyncDedicatedForumUsesBothOverloadsAndCompensationAudit)),
                    "Expected the dedicated forum to appear in the authenticated homepage-dislike listing after the name-based dislike mutation.");

                var undislikedByFid = await client.Forums.UndislikeAsync(context.ForumId);
                Assert.IsTrue(undislikedByFid,
                    $"Expected the dedicated forum '{context.ForumName}' to accept a temporary homepage-undislike mutation via the fid overload.");
                Assert.IsFalse(
                    await IsForumDislikedAsync(
                        client,
                        context,
                        nameof(DislikeAndUndislikeAsyncDedicatedForumUsesBothOverloadsAndCompensationAudit)),
                    "Expected the dedicated forum to disappear from the authenticated homepage-dislike listing after the fid-based undislike mutation.");

                var dislikedByFid = await client.Forums.DislikeAsync(context.ForumId);
                Assert.IsTrue(dislikedByFid,
                    $"Expected the dedicated forum '{context.ForumName}' to accept a temporary homepage-dislike mutation via the fid overload.");
                Assert.IsTrue(
                    await IsForumDislikedAsync(
                        client,
                        context,
                        nameof(DislikeAndUndislikeAsyncDedicatedForumUsesBothOverloadsAndCompensationAudit)),
                    "Expected the dedicated forum to reappear in the authenticated homepage-dislike listing after the fid-based dislike mutation.");

                var dislikedArtifact = scope.Compensation.RecordMutatedArtifact(
                    OnlineTestStageCategories.ForumExtensions,
                    "forum-dislike",
                    context.ForumId.ToString(CultureInfo.InvariantCulture),
                    $"temporary homepage dislike of dedicated forum '{context.ForumName}' via the fid overload");
                scope.Compensation.Register(
                    dislikedArtifact,
                    "undo dedicated forum dislike by name",
                    "forum undisliked",
                    cancellationToken => UndislikeByNameAsync(client, context, cancellationToken));

                await scope.Compensation.ExecuteAsync();

                var audit = scope.Compensation.GetLastAudit();
                Assert.IsNotNull(audit);
                Assert.IsTrue(audit.Succeeded,
                    "Expected the ForumExtensions safe scenario to reconcile the dedicated forum dislike mutation.");
                Assert.HasCount(1, audit.RecordedArtifacts);
                Assert.HasCount(1, audit.CompensationResults);
                Assert.IsEmpty(audit.UnreconciledArtifacts);
                Assert.AreEqual("forum undisliked", audit.CompensationResults[0].CompensationOutcome);

                var auditDisplay = string.Join(global::System.Environment.NewLine, audit.ToDisplayLines());
                Assert.Contains(context.ForumName, auditDisplay);
                Assert.Contains("unreconciled: none", auditDisplay);
                Assert.IsFalse(
                    await IsForumDislikedAsync(
                        client,
                        context,
                        nameof(DislikeAndUndislikeAsyncDedicatedForumUsesBothOverloadsAndCompensationAudit)),
                    "Expected the dedicated forum dislike mutation to be undone once compensation completed.");
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

    private static void AssertForumLevelShape(ForumLevelInfo forumLevel)
    {
        Assert.IsNotNull(forumLevel);
        Assert.IsGreaterThanOrEqualTo(0, forumLevel.UserLevel);
        Assert.IsNotNull(forumLevel.LevelName);
    }

    private static void AssertImageShape(ForumImage image)
    {
        Assert.IsNotNull(image);
        Assert.IsFalse(image.IsEmpty, "Expected the image probe to resolve a non-empty image payload.");
        Assert.IsPositive(image.Data.Length);
        Assert.IsPositive(image.Width);
        Assert.IsPositive(image.Height);
    }

    private static void AssertMemberUsersShape(MemberUsers memberUsers)
    {
        Assert.IsNotNull(memberUsers);
        Assert.IsNotNull(memberUsers.Page);
        Assert.IsGreaterThanOrEqualTo(1, memberUsers.Page.CurrentPage);
        Assert.IsGreaterThanOrEqualTo(0, memberUsers.Count);

        if (memberUsers.Count > 0)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(memberUsers[0].UserName));
            Assert.IsFalse(string.IsNullOrWhiteSpace(memberUsers[0].Portrait));
            Assert.IsGreaterThanOrEqualTo(0, memberUsers[0].Level);
        }
    }

    private static void AssertRankForumsShape(RankForums ranks)
    {
        Assert.IsNotNull(ranks);
        Assert.IsGreaterThanOrEqualTo(1, ranks.Page.CurrentPage);
        Assert.IsGreaterThanOrEqualTo(1, ranks.Page.TotalPage);
        if (ranks.Count > 0)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(ranks[0].Fname));
            Assert.IsGreaterThanOrEqualTo(0, ranks[0].MemberNum);
        }
    }

    private static void AssertRecomStatusShape(RecomStatus recomStatus)
    {
        Assert.IsNotNull(recomStatus);
        Assert.IsGreaterThanOrEqualTo(0, recomStatus.TotalRecommendNum);
        Assert.IsGreaterThanOrEqualTo(0, recomStatus.UsedRecommendNum);
        Assert.IsGreaterThanOrEqualTo(recomStatus.TotalRecommendNum, recomStatus.UsedRecommendNum);
    }

    private static void AssertStatisticsShape(ForumStatistics statistics)
    {
        Assert.IsNotNull(statistics);
        Assert.IsNotNull(statistics.View);
        Assert.IsNotNull(statistics.Thread);
        Assert.IsNotNull(statistics.NewMember);
        Assert.IsNotNull(statistics.Post);
        Assert.IsNotNull(statistics.SignRatio);
        Assert.IsNotNull(statistics.AvgTime);
        Assert.IsNotNull(statistics.AvgTimes);
        Assert.IsNotNull(statistics.Recommend);
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

    private static void EnsureSafeStoken(OnlineExecutionScope scope, string operationName)
    {
        if (!scope.Safe.Account.IsConfigured || string.IsNullOrWhiteSpace(scope.Safe.Account.Stoken))
        {
            Assert.Inconclusive(
                $"Skipping {operationName}: this forum-extension read path requires a safe account with STOKEN-backed authenticated access.");
        }
    }

    private static async Task<ImageProbeContext> RequireImageProbeAsync(
        TiebaClient client,
        DedicatedForumContext context,
        string operationName)
    {
        for (var threadPage = 1; threadPage <= ThreadDiscoveryPages; threadPage++)
        {
            var threads = await client.Threads.GetThreadsAsync(context.ForumName, threadPage, ThreadDiscoveryPageSize, ThreadSortType.Reply);
            foreach (var thread in threads.Objs.Take(ThreadCandidateCount))
            {
                if (TryCreateImageProbe(thread.Content.Images, out var threadImageProbe))
                    return threadImageProbe;

                var posts = await client.Threads.GetPostsAsync(thread.Tid, 1, PostDiscoveryPageSize, PostSortType.Hot, withComments: true, commentRn: 2);
                foreach (var post in posts.Objs)
                {
                    if (TryCreateImageProbe(post.Content.Images, out var postImageProbe))
                        return postImageProbe;

                    foreach (var comment in post.Comments)
                    {
                        if (TryCreateImageProbe(comment.Content.Images, out var commentImageProbe))
                            return commentImageProbe;
                    }
                }
            }

            if (!threads.HasMore)
                break;
        }

        Assert.Inconclusive(
            $"Skipping {operationName}: the dedicated safe forum '{context.ForumName}' did not expose a hash-backed image probe in the bounded discovery window. Point {OnlineTestEnvironmentVariables.SafeAssetsForumQuery} or {OnlineTestEnvironmentVariables.SafeAssetsForumName} at a safe forum with visible image content.");
        return default;
    }

    private static long RequireTargetUserId(OnlineExecutionScope scope, string operationName)
    {
        if (scope.Safe.Assets.TargetUserId is > 0)
            return scope.Safe.Assets.TargetUserId.Value;

        Assert.Inconclusive(
            $"Skipping {operationName}: forum-extension read coverage requires a dedicated target user id. Set {OnlineTestEnvironmentVariables.SafeAssetsTargetUserId} before running this scenario.");
        return default;
    }

    private static string RequireTargetPortrait(OnlineExecutionScope scope, string operationName)
    {
        if (!string.IsNullOrWhiteSpace(scope.Safe.Assets.TargetPortrait))
            return scope.Safe.Assets.TargetPortrait;

        Assert.Inconclusive(
            $"Skipping {operationName}: forum media coverage requires a dedicated portrait asset. Set {OnlineTestEnvironmentVariables.SafeAssetsTargetPortrait} before running this scenario.");
        return string.Empty;
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

    private static async Task<SquareForumsProbe> QuerySquareForumsAsync(
        OnlineExecutionScope scope,
        TiebaClient client,
        DedicatedForumContext context,
        string operationName)
    {
        var probeCandidates = new[]
            {
                context.ForumName,
                context.ForumSelector,
                scope.Safe.Assets.TargetUserName
            }
            .Where(static candidate => !string.IsNullOrWhiteSpace(candidate))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (probeCandidates.Length == 0)
        {
            Assert.Inconclusive(
                $"Skipping {operationName}: no dedicated safe string probes were available for square-forum coverage. Provide at least one of {OnlineTestEnvironmentVariables.SafeAssetsForumQuery}, {OnlineTestEnvironmentVariables.SafeAssetsForumName}, or {OnlineTestEnvironmentVariables.SafeAssetsTargetUserName}.");
        }

        var failureReasons = new List<string>();
        foreach (var probeCandidate in probeCandidates)
        {
            try
            {
                var squareForums = await client.Forums.GetSquareForumsAsync(probeCandidate, 1, 20);
                return new SquareForumsProbe(probeCandidate, squareForums);
            }
            catch (TieBaServerException ex)
            {
                failureReasons.Add($"'{probeCandidate}': {ex.Message}");
            }
            catch (TiebaTransportException ex)
            {
                failureReasons.Add($"'{probeCandidate}': {ex.Message}");
            }
        }

        Assert.Inconclusive(
            $"Skipping {operationName}: the configured dedicated safe string probes did not map to an accepted square-forum category. Tried {string.Join(", ", failureReasons)}.");
        return default;
    }

    private static async Task<bool> IsForumDislikedAsync(
        TiebaClient client,
        DedicatedForumContext context,
        string operationName)
    {
        for (var page = 1; page <= DislikeScanMaxPages; page++)
        {
            var dislikes = await client.Forums.GetDislikeForumsAsync(page, DislikeScanPageSize);
            if (dislikes.Any(forum => forum.Fid == context.ForumId
                                      || string.Equals(forum.Fname, context.ForumName, StringComparison.Ordinal)))
            {
                return true;
            }

            if (!dislikes.HasMore)
                return false;
        }

        Assert.Inconclusive(
            $"Skipping {operationName}: the bounded homepage-dislike scan could not conclusively determine whether dedicated forum '{context.ForumName}' is already disliked. Reduce the safe account's dislike list or choose a clearer disposable forum asset.");
        return false;
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

    private static bool TryCreateImageProbe(IEnumerable<FragImage> images, out ImageProbeContext probe)
    {
        foreach (var image in images)
        {
            if (string.IsNullOrWhiteSpace(image.Hash))
                continue;

            var imageUrl = new[] { image.OriginSrc, image.BigSrc, image.Src }
                .FirstOrDefault(candidate => Uri.TryCreate(candidate, UriKind.Absolute, out _));
            if (string.IsNullOrWhiteSpace(imageUrl))
                continue;

            probe = new ImageProbeContext(image.Hash, imageUrl);
            return true;
        }

        probe = default!;
        return false;
    }

    private static async ValueTask UndislikeByNameAsync(
        TiebaClient client,
        DedicatedForumContext context,
        CancellationToken cancellationToken)
    {
        var undisliked = await client.Forums.UndislikeAsync(context.ForumName, cancellationToken);
        if (!undisliked)
        {
            throw new InvalidOperationException(
                $"Expected to undo the temporary homepage-dislike mutation for dedicated forum '{context.ForumName}'.");
        }
    }

    private static async ValueTask UnfollowByNameAsync(
        TiebaClient client,
        DedicatedForumContext context,
        CancellationToken cancellationToken)
    {
        var unfollowed = await client.Forums.UnfollowAsync(context.ForumName, cancellationToken);
        if (!unfollowed)
        {
            throw new InvalidOperationException(
                $"Expected to undo the temporary safe follow mutation for dedicated forum '{context.ForumName}'.");
        }
    }

    private sealed record DedicatedForumContext(string ForumSelector, string ForumName, ulong ForumId);

    private sealed record ImageProbeContext(string Hash, string ImageUrl);

    private sealed record SquareForumsProbe(string CategoryName, SquareForums Result);
}
