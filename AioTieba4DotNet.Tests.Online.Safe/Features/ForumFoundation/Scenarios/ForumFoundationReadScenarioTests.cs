using System.Threading.Tasks;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Tests.Infrastructure.Configuration;
using AioTieba4DotNet.Tests.Infrastructure.Contracts;
using AioTieba4DotNet.Tests.Infrastructure.Execution;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Online.Safe.Features.ForumFoundation.Scenarios;

[TestClass]
[TestCategory(OnlineTestSuiteCategories.SafeOrdered)]
[TestCategory(OnlineTestFeatureCategories.ForumFoundation)]
[TestCategory(OnlineTestTierCategories.Safe)]
[TestCategory(OnlineTestStageCategories.ForumFoundation)]
[TestSubject(typeof(TiebaClient))]
public sealed class ForumFoundationReadScenarioTests : OnlineSafeExecutionTestBase
{
    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetFidAsync)]
    [TestCategory(OnlineTestApiCategories.ForumsGetForumAsync)]
    [TestCategory(OnlineTestApiCategories.ForumsGetDetailAsync)]
    public Task GetForumAsyncSafeForumQueryReturnsCanonicalForumIdentity()
    {
        return ExecuteSafeAsync(
            "forum foundation get-forum sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var context = await ResolveDedicatedForumContextAsync(
                    scope,
                    client,
                    nameof(GetForumAsyncSafeForumQueryReturnsCanonicalForumIdentity));
                var forumId = await client.Forums.GetFidAsync(context.ForumName);
                var forumDetail = await client.Forums.GetDetailAsync(context.ForumId);

                Assert.IsNotNull(context.Forum);
                Assert.IsPositive(context.Forum.Fid);
                Assert.AreEqual((long)context.ForumId, context.Forum.Fid);
                Assert.AreEqual(context.ForumName, context.Forum.Fname);
                Assert.AreEqual(context.ForumId, forumId);

                Assert.IsNotNull(forumDetail);
                Assert.AreEqual(context.ForumName, forumDetail.Fname);
                Assert.IsGreaterThan(0UL, forumDetail.Fid);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetFnameAsync)]
    [TestCategory(OnlineTestApiCategories.ForumsGetDetailAsync)]
    [TestCategory(OnlineTestApiCategories.ThreadsGetThreadsAsync)]
    public Task ForumFoundationReadsStableThreadListingIdentityReturnConsistentForumMetadata()
    {
        return ExecuteSafeAsync(
            "forum foundation read scenario sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var context = await ResolveDedicatedForumContextAsync(
                    scope,
                    client,
                    nameof(ForumFoundationReadsStableThreadListingIdentityReturnConsistentForumMetadata));
                var forumName = await client.Forums.GetFnameAsync(context.ForumId);
                var forumDetail = await client.Forums.GetDetailAsync(context.ForumName);
                var threads = await client.Threads.GetThreadsAsync(context.ForumSelector);

                Assert.AreEqual(context.ForumName, forumName);
                Assert.IsNotNull(forumDetail);
                Assert.AreEqual(context.ForumName, forumDetail.Fname);
                Assert.IsGreaterThan(0UL, forumDetail.Fid);
                Assert.IsNotNull(threads);
                Assert.IsNotNull(threads.Page);
                Assert.IsNotNull(threads.Forum);
                Assert.IsNotNull(threads.TabDictionary);
                Assert.AreEqual((long)context.ForumId, threads.Forum.Fid);
                Assert.AreEqual(context.ForumName, threads.Forum.Fname);
                Assert.IsNotNull(threads.Objs);
            });
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
                $"Skipping {operationName}: safe forum foundation coverage requires an explicit dedicated forum asset. Set {OnlineTestEnvironmentVariables.SafeAssetsForumQuery} or {OnlineTestEnvironmentVariables.SafeAssetsForumName} instead of relying on a public fallback.");
        }

        var forum = await client.Forums.GetForumAsync(forumSelector);
        Assert.IsNotNull(forum);
        Assert.IsPositive(forum.Fid);
        Assert.IsFalse(string.IsNullOrWhiteSpace(forum.Fname));

        return new DedicatedForumContext(forumSelector, forum.Fname, checked((ulong)forum.Fid), forum);
    }

    private sealed record DedicatedForumContext(string ForumSelector, string ForumName, ulong ForumId, Forum Forum);
}
