#nullable enable
using System;
using System.Threading.Tasks;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Tests.Platform.Configuration;
using AioTieba4DotNet.Tests.Platform.Contracts;
using AioTieba4DotNet.Tests.Platform.Execution;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Online.Tiers.Safe.Features.ForumFoundation.Scenarios;

[TestClass]
[TestCategory(OnlineTestSuiteCategories.SafeOrdered)]
[TestCategory(OnlineTestFeatureCategories.ForumFoundation)]
[TestCategory(OnlineTestTierCategories.Safe)]
[TestCategory(OnlineTestStageCategories.ForumFoundation)]
[TestSubject(typeof(TiebaClient))]
public sealed class ForumFoundationReadScenarioTests : OnlineSafeExecutionTestBase
{
    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetForumAsync)]
    public Task GetForumAsyncSafeForumQueryReturnsCanonicalForumIdentity()
    {
        return ExecuteSafeAsync(
            "forum foundation get-forum sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var fixture = RequireDedicatedForumFixture(
                    scope,
                    nameof(GetForumAsyncSafeForumQueryReturnsCanonicalForumIdentity));
                var forum = await client.Forums.GetForumAsync(fixture.ForumSelector);

                AssertForumShape(fixture, forum);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetFidAsync)]
    public Task GetFidAsyncDedicatedForumNameReturnsCanonicalForumId()
    {
        return ExecuteSafeAsync(
            "forum foundation get-fid sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetFidAsyncDedicatedForumNameReturnsCanonicalForumId);
                var forumName = RequireDedicatedForumName(scope, operationName);
                var expectedForumId = RequireDedicatedForumId(scope, operationName);
                var forumId = await client.Forums.GetFidAsync(forumName);

                Assert.AreEqual(expectedForumId, forumId);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetDetailAsync)]
    public Task GetDetailAsyncDedicatedForumIdReturnsCanonicalForumIdentity()
    {
        return ExecuteSafeAsync(
            "forum foundation detail by fid sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetDetailAsyncDedicatedForumIdReturnsCanonicalForumIdentity);
                var fixture = RequireDedicatedForumFixture(scope, operationName);
                var forumId = RequireDedicatedForumId(scope, operationName);
                var forumDetail = await client.Forums.GetDetailAsync(forumId);

                AssertForumDetailShape(fixture with { ForumId = forumId }, forumDetail);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetFnameAsync)]
    public Task GetFnameAsyncDedicatedForumIdReturnsCanonicalForumName()
    {
        return ExecuteSafeAsync(
            "forum foundation get-fname sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetFnameAsyncDedicatedForumIdReturnsCanonicalForumName);
                var forumId = RequireDedicatedForumId(scope, operationName);
                var expectedForumName = RequireDedicatedForumName(scope, operationName);
                var forumName = await client.Forums.GetFnameAsync(forumId);

                Assert.AreEqual(expectedForumName, forumName);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ForumsGetDetailAsync)]
    public Task GetDetailAsyncDedicatedForumNameReturnsCanonicalForumIdentity()
    {
        return ExecuteSafeAsync(
            "forum foundation detail by name sample",
            async scope =>
            {
                using var client = CreateClient(scope);
                var operationName = nameof(GetDetailAsyncDedicatedForumNameReturnsCanonicalForumIdentity);
                var fixture = RequireDedicatedForumFixture(scope, operationName);
                var forumName = RequireDedicatedForumName(scope, operationName);
                var forumDetail = await client.Forums.GetDetailAsync(forumName);

                AssertForumDetailShape(fixture with { ForumName = forumName }, forumDetail);
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsGetThreadsAsync)]
    public Task GetThreadsAsyncDedicatedForumSelectorReturnsStableForumMetadata()
    {
        return ExecuteSafeAsync(
            "forum foundation threads sample",
            async scope =>
            {
                using var client = CreateClient(scope, TiebaTransportMode.Auto);
                var fixture = RequireDedicatedForumFixture(
                    scope,
                    nameof(GetThreadsAsyncDedicatedForumSelectorReturnsStableForumMetadata));
                var threads = await RunWebSocketForumFoundationOrInconclusiveAsync(
                    () => client.Threads.GetThreadsAsync(fixture.ForumSelector));

                AssertThreadsShape(fixture, threads);
            });
    }

    private static async Task<T> RunWebSocketForumFoundationOrInconclusiveAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (TiebaTransportException exception) when (IsWebSocketTransportEnvironmentGate(exception))
        {
            Assert.Inconclusive(
                $"Skipping forum foundation websocket thread read in this environment: websocket transport is not currently available ({exception.Message}).");
            throw;
        }
    }

    private static bool IsWebSocketTransportEnvironmentGate(TiebaTransportException exception)
    {
        return exception.Message.Contains("WebSocket connect/handshake failed before the request pipeline became available.", StringComparison.Ordinal)
               || exception.Message.Contains("WebSocket request", StringComparison.Ordinal)
               || exception.Message.Contains("The WebSocket receive loop failed", StringComparison.Ordinal)
               || exception.Message.Contains("The WebSocket heartbeat loop failed", StringComparison.Ordinal)
               || exception.Message.Contains("No such host is known", StringComparison.OrdinalIgnoreCase)
               || exception.Message.Contains("sofire.baidu.com", StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertForumShape(DedicatedForumFixture fixture, Forum forum)
    {
        Assert.IsNotNull(forum);
        Assert.IsPositive(forum.Fid);
        Assert.IsFalse(string.IsNullOrWhiteSpace(forum.Fname));

        if (fixture.ForumId is { } forumId)
            Assert.AreEqual((long)forumId, forum.Fid);

        if (!string.IsNullOrWhiteSpace(fixture.ForumName))
            Assert.AreEqual(fixture.ForumName, forum.Fname);
    }

    private static void AssertForumDetailShape(DedicatedForumFixture fixture, ForumDetail forumDetail)
    {
        Assert.IsNotNull(forumDetail);
        Assert.IsGreaterThan(0UL, forumDetail.Fid);

        if (fixture.ForumId is { } forumId)
            Assert.AreEqual(forumId, forumDetail.Fid);

        if (!string.IsNullOrWhiteSpace(fixture.ForumName))
            Assert.AreEqual(fixture.ForumName, forumDetail.Fname);
    }

    private static void AssertThreadsShape(DedicatedForumFixture fixture, Threads threads)
    {
        Assert.IsNotNull(threads);
        Assert.IsNotNull(threads.Page);
        Assert.IsNotNull(threads.Forum);
        Assert.IsNotNull(threads.TabDictionary);
        Assert.IsPositive(threads.Forum.Fid);
        Assert.IsFalse(string.IsNullOrWhiteSpace(threads.Forum.Fname));

        if (fixture.ForumId is { } forumId)
            Assert.AreEqual((long)forumId, threads.Forum.Fid);

        if (!string.IsNullOrWhiteSpace(fixture.ForumName))
            Assert.AreEqual(fixture.ForumName, threads.Forum.Fname);

        Assert.IsNotNull(threads.Objs);
    }

    private static TiebaClient CreateClient(OnlineExecutionScope scope, TiebaTransportMode transportMode = TiebaTransportMode.Http)
    {
        var options = new TiebaOptions
        {
            Bduss = scope.Safe.Account.IsConfigured ? scope.Safe.Account.Bduss : null,
            Stoken = scope.Safe.Account.IsConfigured ? scope.Safe.Account.Stoken : null,
            TransportMode = transportMode
        };

        return new TiebaClient(options);
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
                $"Skipping {operationName}: safe forum foundation coverage requires an explicit dedicated forum asset. Set {OnlineTestEnvironmentVariables.SafeAssetsForumQuery} or {OnlineTestEnvironmentVariables.SafeAssetsForumName} instead of relying on a public fallback.");
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
            $"Skipping {operationName}: this forum foundation path requires a canonical dedicated forum name. Set {OnlineTestEnvironmentVariables.SafeAssetsForumName} to the forum display name rather than a numeric selector before running the scenario.");
        return string.Empty;
    }

    private static ulong RequireDedicatedForumId(OnlineExecutionScope scope, string operationName)
    {
        if (scope.Safe.Assets.ForumId is > 0)
            return (ulong)scope.Safe.Assets.ForumId.Value;

        Assert.Inconclusive(
            $"Skipping {operationName}: this forum foundation path requires a dedicated numeric forum id. Set {OnlineTestEnvironmentVariables.SafeAssetsForumId} before running the fid-overload scenario.");
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

    private sealed record DedicatedForumFixture(string ForumSelector, string? ForumName, ulong? ForumId);
}
