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
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Tests.Platform.Configuration;
using AioTieba4DotNet.Tests.Platform.Contracts;
using AioTieba4DotNet.Tests.Platform.Execution;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Online.Tiers.Safe.Features.ForumExtensions.Scenarios;

[TestClass]
[TestCategory(OnlineTestSuiteCategories.SafeOrdered)]
[TestCategory(OnlineTestFeatureCategories.ForumExtensions)]
[TestCategory(OnlineTestTierCategories.Safe)]
[TestCategory(OnlineTestStageCategories.ForumExtensions)]
[TestSubject(typeof(TiebaClient))]
public sealed class ForumExtensionsScenarioTests : OnlineSafeExecutionTestBase
{
    private const int SearchProbeLength = 2;
    private const int DislikeListingPageSize = 20;
    private const int MutationRetryAttempts = 6;
    private static readonly TimeSpan MutationRetryDelay = TimeSpan.FromSeconds(5);

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsSearchExactAsync)]
    public Task SearchExactAsyncStableForumIdReturnsSearchPageShape()
    {
        return ExecuteSafeAsync(
            "forum extensions exact-search by-fid sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(SearchExactAsyncStableForumIdReturnsSearchPageShape);
                var fixture = RequireDedicatedForumFixture(scope, operationName);
                var forumId = RequireDedicatedForumId(scope, operationName);
                var searches = await client.Forums.SearchExactAsync(forumId, CreateSearchProbeQuery(fixture), 1, 20);

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
                var operationName = nameof(SearchExactAsyncStableForumReturnsSearchPageShape);
                var fixture = RequireDedicatedForumFixture(scope, operationName);
                var forumName = RequireDedicatedForumName(scope, operationName);
                var searches = await client.Forums.SearchExactAsync(forumName, CreateSearchProbeQuery(fixture), 1, 20);

                Assert.IsNotNull(searches);
                Assert.IsNotNull(searches.Page);
                Assert.IsGreaterThanOrEqualTo(0, searches.Page.TotalCount);
                Assert.IsGreaterThanOrEqualTo(0, searches.Page.TotalPage);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetLastReplyersAsync)]
    public Task GetLastReplyersAsyncStableForumNameReturnsThreadPageShape()
    {
        return ExecuteSafeAsync(
            "forum extensions last-replyers by name sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetLastReplyersAsyncStableForumNameReturnsThreadPageShape);
                var fixture = RequireDedicatedForumFixture(scope, operationName);
                var forumName = RequireDedicatedForumName(scope, operationName);
                var threads = await client.Forums.GetLastReplyersAsync(forumName, 1, 20);

                AssertLastReplyersShape(threads, expectedForumName: forumName);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetLastReplyersAsync)]
    public Task GetLastReplyersAsyncStableForumFidReturnsThreadPageShape()
    {
        return ExecuteSafeAsync(
            "forum extensions last-replyers by fid sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetLastReplyersAsyncStableForumFidReturnsThreadPageShape);
                var fixture = RequireDedicatedForumFixture(scope, operationName);
                var forumId = RequireDedicatedForumId(scope, operationName);
                var threads = await client.Forums.GetLastReplyersAsync(forumId, 1, 20);

                AssertLastReplyersShape(threads, expectedForumId: forumId);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetRankForumsAsync)]
    public Task GetRankForumsAsyncStableForumNameReturnsRankPageShape()
    {
        return ExecuteSafeAsync(
            "forum extensions rank read by name",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetRankForumsAsyncStableForumNameReturnsRankPageShape);
                var forumName = RequireDedicatedForumName(scope, operationName);
                var ranks = await client.Forums.GetRankForumsAsync(forumName, 1, ForumRankType.Weekly);

                AssertRankForumsShape(ranks);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetRankForumsAsync)]
    public Task GetRankForumsAsyncStableForumFidReturnsRankPageShape()
    {
        return ExecuteSafeAsync(
            "forum extensions rank read by fid",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetRankForumsAsyncStableForumFidReturnsRankPageShape);
                var forumId = RequireDedicatedForumId(scope, operationName);
                var ranks = await client.Forums.GetRankForumsAsync(forumId, 1, ForumRankType.Weekly);

                AssertRankForumsShape(ranks);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetFollowForumsAsync)]
    public Task GetFollowForumsAsyncDedicatedTargetUserReturnsCollectionShape()
    {
        return ExecuteSafeAsync(
            "forum extensions get-follow-forums sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetFollowForumsAsyncDedicatedTargetUserReturnsCollectionShape);
                EnsureSafeStoken(scope, operationName);
                var targetUserId = RequireTargetUserId(scope, operationName);
                var followForums = await client.Forums.GetFollowForumsAsync(targetUserId, 1, 20);

                Assert.IsNotNull(followForums);
                if (followForums.Count > 0)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(followForums[0].Fname));
                    Assert.IsPositive(followForums[0].Fid);
                }
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetSelfFollowForumsV1Async)]
    public Task GetSelfFollowForumsV1AsyncAuthenticatedAccountReturnsPageShape()
    {
        return ExecuteSafeAsync(
            "forum extensions self-follow-v1 sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetSelfFollowForumsV1AsyncAuthenticatedAccountReturnsPageShape);
                EnsureSafeStoken(scope, operationName);
                var selfFollowForumsV1 = await client.Forums.GetSelfFollowForumsV1Async(1, 20);

                Assert.IsNotNull(selfFollowForumsV1);
                Assert.IsNotNull(selfFollowForumsV1.Page);
                Assert.IsGreaterThanOrEqualTo(1, selfFollowForumsV1.Page.CurrentPage);
                Assert.IsGreaterThanOrEqualTo(0, selfFollowForumsV1.Count);
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetCidAsync)]
    public Task GetCidAsyncDedicatedForumNameReturnsZero()
    {
        return ExecuteSafeAsync(
            "forum extensions get-cid by name sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetCidAsyncDedicatedForumNameReturnsZero);
                var forumName = RequireDedicatedForumName(scope, operationName);
                var cid = await client.Forums.GetCidAsync(forumName);

                Assert.AreEqual(0, cid);
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetCidAsync)]
    public Task GetCidAsyncDedicatedForumFidReturnsZero()
    {
        return ExecuteSafeAsync(
            "forum extensions get-cid by fid sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetCidAsyncDedicatedForumFidReturnsZero);
                var forumId = RequireDedicatedForumId(scope, operationName);
                var cid = await client.Forums.GetCidAsync(forumId);

                Assert.AreEqual(0, cid);
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetMemberUsersAsync)]
    public Task GetMemberUsersAsyncDedicatedForumNameReturnsShape()
    {
        return ExecuteSafeAsync(
            "forum extensions member-users by name sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetMemberUsersAsyncDedicatedForumNameReturnsShape);
                var forumName = RequireDedicatedForumName(scope, operationName);
                var memberUsers = await client.Forums.GetMemberUsersAsync(forumName, 1);

                AssertMemberUsersShape(memberUsers);
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetMemberUsersAsync)]
    public Task GetMemberUsersAsyncDedicatedForumFidReturnsShape()
    {
        return ExecuteSafeAsync(
            "forum extensions member-users by fid sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetMemberUsersAsyncDedicatedForumFidReturnsShape);
                var forumId = RequireDedicatedForumId(scope, operationName);
                var memberUsers = await client.Forums.GetMemberUsersAsync(forumId, 1);

                AssertMemberUsersShape(memberUsers);
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetRecomStatusAsync)]
    public Task GetRecomStatusAsyncDedicatedForumNameReturnsShapeOrExplicitSkip()
    {
        return ExecuteSafeAsync(
            "forum extensions recom-status by name sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetRecomStatusAsyncDedicatedForumNameReturnsShapeOrExplicitSkip);
                var fixture = RequireDedicatedForumFixture(scope, operationName);
                var forumName = RequireDedicatedForumName(scope, operationName);
                var recomStatus = await GetRecomStatusOrInconclusiveAsync(() => client.Forums.GetRecomStatusAsync(forumName), fixture, operationName);

                AssertRecomStatusShape(recomStatus);
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetRecomStatusAsync)]
    public Task GetRecomStatusAsyncDedicatedForumFidReturnsShapeOrExplicitSkip()
    {
        return ExecuteSafeAsync(
            "forum extensions recom-status by fid sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetRecomStatusAsyncDedicatedForumFidReturnsShapeOrExplicitSkip);
                var fixture = RequireDedicatedForumFixture(scope, operationName);
                var forumId = RequireDedicatedForumId(scope, operationName);
                var recomStatus = await GetRecomStatusOrInconclusiveAsync(() => client.Forums.GetRecomStatusAsync(forumId), fixture, operationName);

                AssertRecomStatusShape(recomStatus);
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetSquareForumsAsync)]
    public Task GetSquareForumsAsyncDedicatedProbeReturnsPageShape()
    {
        return ExecuteSafeAsync(
            "forum extensions square-forums sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetSquareForumsAsyncDedicatedProbeReturnsPageShape);
                var fixture = RequireDedicatedForumFixture(scope, operationName);
                var squareForumsProbe = await QuerySquareForumsAsync(scope, client, fixture, operationName);

                Assert.IsNotNull(squareForumsProbe.Result);
                Assert.IsNotNull(squareForumsProbe.Result.Page);
                Assert.IsGreaterThanOrEqualTo(0, squareForumsProbe.Result.Page.CurrentPage);
                Assert.IsGreaterThanOrEqualTo(0, squareForumsProbe.Result.Count);
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetStatisticsAsync)]
    public Task GetStatisticsAsyncDedicatedForumNameReturnsShape()
    {
        return ExecuteSafeAsync(
            "forum extensions statistics by name sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetStatisticsAsyncDedicatedForumNameReturnsShape);
                var forumName = RequireDedicatedForumName(scope, operationName);
                var statistics = await client.Forums.GetStatisticsAsync(forumName);

                AssertStatisticsShape(statistics);
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetStatisticsAsync)]
    public Task GetStatisticsAsyncDedicatedForumFidReturnsShape()
    {
        return ExecuteSafeAsync(
            "forum extensions statistics by fid sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetStatisticsAsyncDedicatedForumFidReturnsShape);
                var forumId = RequireDedicatedForumId(scope, operationName);
                var statistics = await client.Forums.GetStatisticsAsync(forumId);

                AssertStatisticsShape(statistics);
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetForumLevelAsync)]
    public Task GetForumLevelAsyncDedicatedForumNameReturnsShape()
    {
        return ExecuteSafeAsync(
            "forum extensions forum-level by name sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetForumLevelAsyncDedicatedForumNameReturnsShape);
                var forumName = RequireDedicatedForumName(scope, operationName);
                var forumLevel = await client.Forums.GetForumLevelAsync(forumName);

                AssertForumLevelShape(forumLevel);
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetForumLevelAsync)]
    public Task GetForumLevelAsyncDedicatedForumFidReturnsShape()
    {
        return ExecuteSafeAsync(
            "forum extensions forum-level by fid sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetForumLevelAsyncDedicatedForumFidReturnsShape);
                var forumId = RequireDedicatedForumId(scope, operationName);
                var forumLevel = await client.Forums.GetForumLevelAsync(forumId);

                AssertForumLevelShape(forumLevel);
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetRoomListByFidAsync)]
    public Task GetRoomListByFidAsyncDedicatedForumReturnsCollectionShape()
    {
        return ExecuteSafeAsync(
            "forum extensions room-list sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetRoomListByFidAsyncDedicatedForumReturnsCollectionShape);
                var forumId = RequireDedicatedForumId(scope, operationName);
                var roomList = await client.Forums.GetRoomListByFidAsync(forumId);

                Assert.IsNotNull(roomList);
                Assert.IsGreaterThanOrEqualTo(0, roomList.Count);
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetPortraitAsync)]
    public Task GetPortraitAsyncDedicatedPortraitReturnsImagePayload()
    {
        return ExecuteSafeAsync(
            "forum extensions portrait sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetPortraitAsyncDedicatedPortraitReturnsImagePayload);
                var portrait = RequireTargetPortrait(scope, operationName);
                var portraitImage = await client.Forums.GetPortraitAsync(portrait, ForumImageSize.Small);

                AssertImageShape(portraitImage);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetImageAsync)]
    public Task GetImageAsyncDedicatedForumImageProbeReturnsImagePayload()
    {
        return ExecuteSafeAsync(
            "forum extensions get-image sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetImageAsyncDedicatedForumImageProbeReturnsImagePayload);
                var imageUrl = RequireForumImageUrl(scope, operationName);
                var forumImage = await client.Forums.GetImageAsync(imageUrl);

                AssertImageShape(forumImage);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetImageBytesAsync)]
    public Task GetImageBytesAsyncDedicatedForumImageProbeReturnsRawBytes()
    {
        return ExecuteSafeAsync(
            "forum extensions image-bytes sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetImageBytesAsyncDedicatedForumImageProbeReturnsRawBytes);
                var imageUrl = RequireForumImageUrl(scope, operationName);
                var forumImageBytes = await client.Forums.GetImageBytesAsync(imageUrl);

                Assert.IsFalse(forumImageBytes.IsEmpty, "Expected the forum image byte probe to download raw bytes.");
                Assert.IsPositive(forumImageBytes.Data.Length);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetImageByHashAsync)]
    public Task GetImageByHashAsyncDedicatedForumImageProbeReturnsImagePayload()
    {
        return ExecuteSafeAsync(
            "forum extensions image-by-hash sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetImageByHashAsyncDedicatedForumImageProbeReturnsImagePayload);
                var imageHash = RequireForumImageHash(scope, operationName);
                var forumImageByHash = await client.Forums.GetImageByHashAsync(imageHash, ForumImageSize.Small);

                AssertImageShape(forumImageByHash);
            });
    }

    private static async Task<RecomStatus> GetRecomStatusOrInconclusiveAsync(
        Func<Task<RecomStatus>> action,
        DedicatedForumFixture fixture,
        string operationName)
    {
        try
        {
            return await action();
        }
        catch (TieBaServerException exception) when (exception.Code == 2121024)
        {
            var forumDisplayName = GetForumDisplayName(fixture);
            Assert.Inconclusive(
                $"Skipping {operationName}: the dedicated safe forum '{forumDisplayName}' does not grant manager visibility for recommendation-status reads in this environment. Point {OnlineTestEnvironmentVariables.SafeAssetsForumQuery} or {OnlineTestEnvironmentVariables.SafeAssetsForumName} at a dedicated forum where the safe account has the manager surface required by Forums.GetRecomStatusAsync.");
            throw;
        }
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetSelfFollowForumsAsync)]
    public Task GetSelfFollowForumsAsyncAuthenticatedAccountReturnsCollectionShape()
    {
        return ExecuteSafeAsync(
            "forum extensions self-follow sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var follows = await client.Forums.GetSelfFollowForumsAsync(1, 200);

                Assert.IsNotNull(follows);
                if (follows.Count > 0)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(follows[0].Fname));
                    Assert.IsPositive(follows[0].Fid);
                }
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsFollowAsync)]
    public Task FollowAsyncDedicatedForumNameUsesCompensationAudit()
    {
        return ExecuteSafeAsync(
            "forum extensions follow by name lifecycle",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(FollowAsyncDedicatedForumNameUsesCompensationAudit);
                var fixture = RequireDedicatedForumFixture(scope, operationName);
                var forumName = RequireDedicatedForumName(scope, operationName);
                var followedByName = await RunForumMutationOrInconclusiveAsync(
                    () => client.Forums.FollowAsync(forumName),
                    operationName,
                    "the forum follow endpoint is rate-limited in this environment");
                if (!followedByName)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: dedicated forum '{GetForumDisplayName(fixture)}' did not accept the follow mutation. Reconfigure the disposable forum so it starts unfollowed and can truthfully prove the name-overload follow path without follow-list probe reads.");
                }

                RegisterForumFollowCompensationByName(scope, client, fixture, forumName, $"temporary follow of dedicated forum '{GetForumDisplayName(fixture)}' via the name overload");

                await scope.Compensation.ExecuteAsync();
                AssertSingleForumAudit(scope, GetForumDisplayName(fixture), "forum unfollowed", "forum-follow");
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsUnfollowAsync)]
    public Task UnfollowAsyncDedicatedForumFidRestoresSetupFollowAndPublishesCompensationAudit()
    {
        return ExecuteSafeAsync(
            "forum extensions unfollow by fid lifecycle",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(UnfollowAsyncDedicatedForumFidRestoresSetupFollowAndPublishesCompensationAudit);
                var fixture = RequireDedicatedForumFixture(scope, operationName);
                var forumId = RequireDedicatedForumId(scope, operationName);
                var setupFollowed = await RunForumMutationOrInconclusiveAsync(
                    () => client.Forums.FollowAsync(forumId),
                    operationName,
                    "the preparatory forum follow endpoint is rate-limited in this environment");
                if (!setupFollowed)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: dedicated forum '{GetForumDisplayName(fixture)}' did not accept the preparatory follow mutation needed for fid-overload unfollow coverage.");
                }

                RegisterForumFollowCompensationByFid(scope, client, fixture, forumId, $"setup follow of dedicated forum '{GetForumDisplayName(fixture)}' before fid-overload unfollow coverage");

                var unfollowedByFid = await RunForumMutationOrInconclusiveAsync(
                    () => client.Forums.UnfollowAsync(forumId),
                    operationName,
                    "the forum unfollow endpoint is rate-limited in this environment");
                if (!unfollowedByFid)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: dedicated forum '{GetForumDisplayName(fixture)}' did not accept the fid-overload unfollow mutation. Reconfigure the disposable forum so it starts followed and can truthfully prove the direct unfollow path without setup follow calls.");
                }

                await scope.Compensation.ExecuteAsync();
                AssertSingleForumAudit(scope, GetForumDisplayName(fixture), "forum unfollowed", "forum-follow");
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsFollowAsync)]
    public Task FollowAsyncDedicatedForumFidUsesCompensationAudit()
    {
        return ExecuteSafeAsync(
            "forum extensions follow by fid lifecycle",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(FollowAsyncDedicatedForumFidUsesCompensationAudit);
                var fixture = RequireDedicatedForumFixture(scope, operationName);
                var forumId = RequireDedicatedForumId(scope, operationName);
                var followedByFid = await RunForumMutationOrInconclusiveAsync(
                    () => client.Forums.FollowAsync(forumId),
                    operationName,
                    "the forum follow endpoint is rate-limited in this environment");
                if (!followedByFid)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: dedicated forum '{GetForumDisplayName(fixture)}' did not accept the fid-overload follow mutation. Reconfigure the disposable forum so it starts unfollowed and can truthfully prove the direct follow path without follow-list probe reads.");
                }

                RegisterForumFollowCompensationByFid(scope, client, fixture, forumId, $"temporary follow of dedicated forum '{GetForumDisplayName(fixture)}' via the fid overload");

                await scope.Compensation.ExecuteAsync();
                AssertSingleForumAudit(scope, GetForumDisplayName(fixture), "forum unfollowed", "forum-follow");
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetDislikeForumsAsync)]
    public Task GetDislikeForumsAsyncAuthenticatedAccountReturnsCollectionShape()
    {
        return ExecuteSafeAsync(
            "forum extensions dislike listing sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var dislikes = await client.Forums.GetDislikeForumsAsync(1, DislikeListingPageSize);

                Assert.IsNotNull(dislikes);
                if (dislikes.Count > 0)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(dislikes[0].Fname));
                    Assert.IsPositive(dislikes[0].Fid);
                }
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsDislikeAsync)]
    public Task DislikeAsyncDedicatedForumNameUsesCompensationAudit()
    {
        return ExecuteSafeAsync(
            "forum extensions dislike by name lifecycle",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(DislikeAsyncDedicatedForumNameUsesCompensationAudit);
                var fixture = RequireDedicatedForumFixture(scope, operationName);
                var forumName = RequireDedicatedForumName(scope, operationName);
                var dislikedByName = await RunForumMutationOrInconclusiveAsync(
                    () => client.Forums.DislikeAsync(forumName),
                    operationName,
                    "the forum dislike endpoint is rate-limited in this environment");
                if (!dislikedByName)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: dedicated forum '{GetForumDisplayName(fixture)}' did not accept the homepage-dislike mutation. Reconfigure the disposable forum so it starts outside the dislike list and can truthfully prove the name-overload path without dislike-list probe reads.");
                }

                RegisterForumDislikeCompensationByName(scope, client, fixture, forumName, $"temporary homepage dislike of dedicated forum '{GetForumDisplayName(fixture)}' via the name overload");

                await scope.Compensation.ExecuteAsync();
                AssertSingleForumAudit(scope, GetForumDisplayName(fixture), "forum undisliked", "forum-dislike");
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsUndislikeAsync)]
    public Task UndislikeAsyncDedicatedForumFidRestoresSetupDislikeAndPublishesCompensationAudit()
    {
        return ExecuteSafeAsync(
            "forum extensions undislike by fid lifecycle",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(UndislikeAsyncDedicatedForumFidRestoresSetupDislikeAndPublishesCompensationAudit);
                var fixture = RequireDedicatedForumFixture(scope, operationName);
                var forumId = RequireDedicatedForumId(scope, operationName);
                var undislikedByFid = await RunForumMutationOrInconclusiveAsync(
                    () => client.Forums.UndislikeAsync(forumId),
                    operationName,
                    "the forum undislike endpoint is rate-limited in this environment");
                if (!undislikedByFid)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: dedicated forum '{GetForumDisplayName(fixture)}' did not accept the fid-overload undislike mutation. Reconfigure the disposable forum so it starts disliked and can truthfully prove the direct undislike path without setup dislike calls.");
                }

                RegisterForumRedislikeCompensationByFid(scope, client, fixture, forumId, $"temporary homepage undislike of dedicated forum '{GetForumDisplayName(fixture)}' via the fid overload");

                await scope.Compensation.ExecuteAsync();
                AssertSingleForumAudit(scope, GetForumDisplayName(fixture), "forum disliked", "forum-dislike");
            },
            OnlineExecutionCapability.Authenticated);
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsDislikeAsync)]
    public Task DislikeAsyncDedicatedForumFidUsesCompensationAudit()
    {
        return ExecuteSafeAsync(
            "forum extensions dislike by fid lifecycle",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(DislikeAsyncDedicatedForumFidUsesCompensationAudit);
                var fixture = RequireDedicatedForumFixture(scope, operationName);
                var forumId = RequireDedicatedForumId(scope, operationName);
                var dislikedByFid = await RunForumMutationOrInconclusiveAsync(
                    () => client.Forums.DislikeAsync(forumId),
                    operationName,
                    "the forum dislike endpoint is rate-limited in this environment");
                if (!dislikedByFid)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: dedicated forum '{GetForumDisplayName(fixture)}' did not accept the fid-overload homepage-dislike mutation. Reconfigure the disposable forum so it starts outside the dislike list and can truthfully prove the direct dislike path without dislike-list probe reads.");
                }

                RegisterForumDislikeCompensationByFid(scope, client, fixture, forumId, $"temporary homepage dislike of dedicated forum '{GetForumDisplayName(fixture)}' via the fid overload");

                await scope.Compensation.ExecuteAsync();
                AssertSingleForumAudit(scope, GetForumDisplayName(fixture), "forum undisliked", "forum-dislike");
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

    private static void AssertLastReplyersShape(LastReplyers threads, string? expectedForumName = null, ulong? expectedForumId = null)
    {
        Assert.IsNotNull(threads);
        Assert.IsNotNull(threads.Page);
        Assert.IsNotNull(threads.Forum);

        if (expectedForumId is { } forumId)
            Assert.AreEqual((long)forumId, threads.Forum.Fid);

        if (!string.IsNullOrWhiteSpace(expectedForumName))
            Assert.AreEqual(expectedForumName, threads.Forum.Fname);
        else
            Assert.IsFalse(string.IsNullOrWhiteSpace(threads.Forum.Fname));

        Assert.IsGreaterThanOrEqualTo(0, threads.Page.TotalCount);
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

    private static string CreateSearchProbeQuery(DedicatedForumFixture fixture)
    {
        var source = !string.IsNullOrWhiteSpace(fixture.ForumName)
            ? fixture.ForumName
            : fixture.ForumSelector;
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

    private static string RequireForumImageUrl(OnlineExecutionScope scope, string operationName)
    {
        if (!string.IsNullOrWhiteSpace(scope.Safe.Assets.ForumImageUrl))
            return scope.Safe.Assets.ForumImageUrl;

        Assert.Inconclusive(
            $"Skipping {operationName}: forum media coverage requires a dedicated image url fixture. Set {OnlineTestEnvironmentVariables.SafeAssetsForumImageUrl} instead of discovering image probes through thread or post reads.");
        return string.Empty;
    }

    private static string RequireForumImageHash(OnlineExecutionScope scope, string operationName)
    {
        if (!string.IsNullOrWhiteSpace(scope.Safe.Assets.ForumImageHash))
            return scope.Safe.Assets.ForumImageHash;

        Assert.Inconclusive(
            $"Skipping {operationName}: forum media hash coverage requires a dedicated image hash fixture. Set {OnlineTestEnvironmentVariables.SafeAssetsForumImageHash} instead of discovering hash probes through thread or post reads.");
        return string.Empty;
    }

    private static DedicatedForumFixture RequireDedicatedForumFixture(
        OnlineExecutionScope scope,
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

        return new DedicatedForumFixture(
            forumSelector,
            TryResolveCanonicalForumName(scope.Safe.Assets.ForumName),
            scope.Safe.Assets.ForumId is > 0 ? (ulong)scope.Safe.Assets.ForumId.Value : null);
    }

    private static string RequireDedicatedForumName(OnlineExecutionScope scope, string operationName)
    {
        if (TryResolveCanonicalForumName(scope.Safe.Assets.ForumName) is { } forumName)
            return forumName;

        Assert.Inconclusive(
            $"Skipping {operationName}: this forum-extensions path requires a canonical dedicated forum name. Set {OnlineTestEnvironmentVariables.SafeAssetsForumName} to the forum display name rather than a numeric selector before running the scenario.");
        return string.Empty;
    }

    private static ulong RequireDedicatedForumId(OnlineExecutionScope scope, string operationName)
    {
        if (scope.Safe.Assets.ForumId is > 0)
            return (ulong)scope.Safe.Assets.ForumId.Value;

        Assert.Inconclusive(
            $"Skipping {operationName}: this forum-extensions fid-overload path requires a dedicated numeric forum id. Set {OnlineTestEnvironmentVariables.SafeAssetsForumId} before running the scenario.");
        return default;
    }

    private static string? TryResolveCanonicalForumName(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return null;

        return ulong.TryParse(candidate, out _)
            ? null
            : candidate;
    }

    private static async Task<SquareForumsProbe> QuerySquareForumsAsync(
        OnlineExecutionScope scope,
        TiebaClient client,
        DedicatedForumFixture fixture,
        string operationName)
    {
        var probeCandidates = new[]
            {
                fixture.ForumName,
                fixture.ForumSelector,
                scope.Safe.Assets.TargetUserName
            }
            .Where(static candidate => !string.IsNullOrWhiteSpace(candidate))
            .Select(static candidate => candidate!)
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

    private static async Task<bool> RunForumMutationOrInconclusiveAsync(
        Func<Task<bool>> action,
        string operationName,
        string reason)
    {
        try
        {
            return await action();
        }
        catch (TieBaServerException exception) when (exception.Code == 1990029)
        {
            Assert.Inconclusive($"Skipping {operationName}: {reason}. {exception.Message}");
            throw;
        }
    }

    private static void AssertSingleForumAudit(
        OnlineExecutionScope scope,
        string forumName,
        string expectedOutcome,
        string expectedArtifactType)
    {
        var audit = scope.Compensation.GetLastAudit();
        Assert.IsNotNull(audit);
        Assert.IsTrue(audit.Succeeded,
            "Expected the ForumExtensions safe scenario to reconcile the dedicated forum mutation.");
        Assert.HasCount(1, audit.RecordedArtifacts);
        Assert.HasCount(1, audit.CompensationResults);
        Assert.IsEmpty(audit.UnreconciledArtifacts);
        Assert.AreEqual(expectedOutcome, audit.CompensationResults[0].CompensationOutcome);
        Assert.AreEqual(expectedArtifactType, audit.RecordedArtifacts[0].ArtifactType);

        var auditDisplay = string.Join(global::System.Environment.NewLine, audit.ToDisplayLines());
        Assert.Contains(forumName, auditDisplay);
        Assert.Contains("unreconciled: none", auditDisplay);
    }

    private static string GetForumDisplayName(DedicatedForumFixture fixture)
    {
        return !string.IsNullOrWhiteSpace(fixture.ForumName)
            ? fixture.ForumName
            : fixture.ForumSelector;
    }

    private static string GetForumArtifactId(DedicatedForumFixture fixture, string fallback)
    {
        return fixture.ForumId is { } forumId
            ? forumId.ToString(CultureInfo.InvariantCulture)
            : fallback;
    }

    private static void RegisterForumDislikeCompensationByName(
        OnlineExecutionScope scope,
        TiebaClient client,
        DedicatedForumFixture fixture,
        string forumName,
        string description)
    {
        var dislikedArtifact = scope.Compensation.RecordMutatedArtifact(
            OnlineTestStageCategories.ForumExtensions,
            "forum-dislike",
            GetForumArtifactId(fixture, forumName),
            description);
        scope.Compensation.Register(
            dislikedArtifact,
            "undo dedicated forum dislike by name",
            "forum undisliked",
            cancellationToken => UndislikeByNameAsync(client, forumName, cancellationToken));
    }

    private static void RegisterForumDislikeCompensationByFid(
        OnlineExecutionScope scope,
        TiebaClient client,
        DedicatedForumFixture fixture,
        ulong forumId,
        string description)
    {
        var dislikedArtifact = scope.Compensation.RecordMutatedArtifact(
            OnlineTestStageCategories.ForumExtensions,
            "forum-dislike",
            GetForumArtifactId(fixture, forumId.ToString(CultureInfo.InvariantCulture)),
            description);
        scope.Compensation.Register(
            dislikedArtifact,
            "undo dedicated forum dislike by fid",
            "forum undisliked",
            cancellationToken => UndislikeByFidAsync(client, forumId, cancellationToken));
    }

    private static void RegisterForumFollowCompensationByName(
        OnlineExecutionScope scope,
        TiebaClient client,
        DedicatedForumFixture fixture,
        string forumName,
        string description)
    {
        var followedArtifact = scope.Compensation.RecordMutatedArtifact(
            OnlineTestStageCategories.ForumExtensions,
            "forum-follow",
            GetForumArtifactId(fixture, forumName),
            description);
        scope.Compensation.Register(
            followedArtifact,
            "undo dedicated forum follow by name",
            "forum unfollowed",
            cancellationToken => UnfollowByNameAsync(client, forumName, cancellationToken));
    }

    private static void RegisterForumFollowCompensationByFid(
        OnlineExecutionScope scope,
        TiebaClient client,
        DedicatedForumFixture fixture,
        ulong forumId,
        string description)
    {
        var followedArtifact = scope.Compensation.RecordMutatedArtifact(
            OnlineTestStageCategories.ForumExtensions,
            "forum-follow",
            GetForumArtifactId(fixture, forumId.ToString(CultureInfo.InvariantCulture)),
            description);
        scope.Compensation.Register(
            followedArtifact,
            "undo dedicated forum follow by fid",
            "forum unfollowed",
            cancellationToken => UnfollowByFidAsync(client, forumId, cancellationToken));
    }

    private static void RegisterForumRedislikeCompensationByFid(
        OnlineExecutionScope scope,
        TiebaClient client,
        DedicatedForumFixture fixture,
        ulong forumId,
        string description)
    {
        var dislikedArtifact = scope.Compensation.RecordMutatedArtifact(
            OnlineTestStageCategories.ForumExtensions,
            "forum-dislike",
            GetForumArtifactId(fixture, forumId.ToString(CultureInfo.InvariantCulture)),
            description);
        scope.Compensation.Register(
            dislikedArtifact,
            "restore dedicated forum dislike by fid",
            "forum disliked",
            cancellationToken => DislikeByFidAsync(client, forumId, cancellationToken));
    }

    private static async ValueTask UndislikeByNameAsync(
        TiebaClient client,
        string forumName,
        CancellationToken cancellationToken)
    {
        await ExecuteForumMutationWithRetryAsync(
            currentCancellationToken => client.Forums.UndislikeAsync(forumName, currentCancellationToken),
            $"Expected to undo the temporary homepage-dislike mutation for dedicated forum '{forumName}'.",
            cancellationToken);
    }

    private static async ValueTask UndislikeByFidAsync(
        TiebaClient client,
        ulong forumId,
        CancellationToken cancellationToken)
    {
        await ExecuteForumMutationWithRetryAsync(
            currentCancellationToken => client.Forums.UndislikeAsync(forumId, currentCancellationToken),
            $"Expected to undo the temporary homepage-dislike mutation for dedicated forum id '{forumId}'.",
            cancellationToken);
    }

    private static async ValueTask DislikeByFidAsync(
        TiebaClient client,
        ulong forumId,
        CancellationToken cancellationToken)
    {
        await ExecuteForumMutationWithRetryAsync(
            currentCancellationToken => client.Forums.DislikeAsync(forumId, currentCancellationToken),
            $"Expected to restore the dedicated forum dislike mutation for id '{forumId}'.",
            cancellationToken);
    }

    private static async ValueTask UnfollowByNameAsync(
        TiebaClient client,
        string forumName,
        CancellationToken cancellationToken)
    {
        await ExecuteForumMutationWithRetryAsync(
            currentCancellationToken => client.Forums.UnfollowAsync(forumName, currentCancellationToken),
            $"Expected to undo the temporary safe follow mutation for dedicated forum '{forumName}'.",
            cancellationToken);
    }

    private static async ValueTask UnfollowByFidAsync(
        TiebaClient client,
        ulong forumId,
        CancellationToken cancellationToken)
    {
        await ExecuteForumMutationWithRetryAsync(
            currentCancellationToken => client.Forums.UnfollowAsync(forumId, currentCancellationToken),
            $"Expected to undo the temporary safe follow mutation for dedicated forum id '{forumId}'.",
            cancellationToken);
    }

    private static async ValueTask ExecuteForumMutationWithRetryAsync(
        Func<CancellationToken, Task<bool>> action,
        string failureMessage,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MutationRetryAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var succeeded = await action(cancellationToken);
                if (succeeded)
                    return;

                throw new InvalidOperationException(failureMessage);
            }
            catch (TieBaServerException exception) when (exception.Code == 1990029 && attempt < MutationRetryAttempts - 1)
            {
                await Task.Delay(MutationRetryDelay, cancellationToken);
            }
        }

        throw new InvalidOperationException(failureMessage);
    }

    private sealed record DedicatedForumFixture(string ForumSelector, string? ForumName, ulong? ForumId);

    private sealed record SquareForumsProbe(string CategoryName, SquareForums Result);
}
