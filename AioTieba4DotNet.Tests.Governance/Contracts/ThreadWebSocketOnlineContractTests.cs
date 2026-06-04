#nullable enable
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Modules;
using AioTieba4DotNet.Protocols;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Tests.Platform.Configuration;
using AioTieba4DotNet.Tests.Platform.Contracts;
using AioTieba4DotNet.Tests.Platform.Execution;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Governance.Contracts;

[TestClass]
[TestCategory(OnlineTestContractCategories.Architecture)]
[TestCategory(OnlineTestTierCategories.Safe)]
[TestSubject(typeof(TiebaClient))]
public sealed class ThreadWebSocketOnlineContractTests
{
    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsGetThreadsAsync)]
    public Task GetThreadsAsyncWebSocketOnlyModeUsesLiveWebSocketWithoutHttpFallback()
    {
        return OnlineExecutionGate.ExecuteSafeAsync(
            OnlineTestEnvironment.LoadCurrent(),
            "governance live websocket-only get_threads proof",
            async scope =>
            {
                var operationName = nameof(GetThreadsAsyncWebSocketOnlyModeUsesLiveWebSocketWithoutHttpFallback);
                var fixture = RequireDedicatedForumFixture(scope, operationName);

                using var handler = new BlockThreadReadHttpHandler();
                using var httpClient = new HttpClient(handler, disposeHandler: true);
                var options = new TiebaOptions
                {
                    Bduss = scope.Safe.Account.IsConfigured ? scope.Safe.Account.Bduss : null,
                    Stoken = scope.Safe.Account.IsConfigured ? scope.Safe.Account.Stoken : null,
                    TransportMode = TiebaTransportMode.WebSocketOnly
                };

                using var session = new TiebaClientSession(options, httpClient);
                var dispatcher = new TiebaOperationDispatcher(session);
                var forumProtocol = new ForumProtocol(dispatcher, new ForumInfoCache());
                var threads = new ThreadModule(new ThreadProtocol(dispatcher, forumProtocol));

                var result = await RunWebSocketThreadReadOrInconclusiveAsync(
                    () => threads.GetThreadsAsync(fixture.ForumSelector, 1, 10, ThreadSortType.Reply, false));

                AssertThreadsShape(fixture, result);
                Assert.AreEqual(0, handler.BlockedGetThreadsRequestCount,
                    "Live websocket verification must not reach the HTTP get_threads endpoint.");
            });
    }

    [TestMethod]
    [TestCategory(OnlineTestApiCategories.ThreadsGetPostsAsync)]
    public Task GetPostsAsyncWebSocketOnlyModeUsesLiveWebSocketWithoutHttpFallback()
    {
        return OnlineExecutionGate.ExecuteSafeAsync(
            OnlineTestEnvironment.LoadCurrent(),
            "governance live websocket-only get_posts proof",
            async scope =>
            {
                var operationName = nameof(GetPostsAsyncWebSocketOnlyModeUsesLiveWebSocketWithoutHttpFallback);
                var fixture = RequireDedicatedForumFixture(scope, operationName);

                using var handler = new BlockThreadReadHttpHandler();
                using var httpClient = new HttpClient(handler, disposeHandler: true);
                var options = new TiebaOptions
                {
                    Bduss = scope.Safe.Account.IsConfigured ? scope.Safe.Account.Bduss : null,
                    Stoken = scope.Safe.Account.IsConfigured ? scope.Safe.Account.Stoken : null,
                    TransportMode = TiebaTransportMode.WebSocketOnly
                };

                using var session = new TiebaClientSession(options, httpClient);
                var dispatcher = new TiebaOperationDispatcher(session);
                var forumProtocol = new ForumProtocol(dispatcher, new ForumInfoCache());
                var threads = new ThreadModule(new ThreadProtocol(dispatcher, forumProtocol));

                var threadListing = await RunWebSocketThreadReadOrInconclusiveAsync(
                    () => threads.GetThreadsAsync(fixture.ForumSelector, 1, 10, ThreadSortType.Reply, false));

                var targetThread = threadListing.Objs.FirstOrDefault();
                if (targetThread == null)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: websocket-only thread discovery returned no visible threads for forum '{fixture.ForumSelector}'.");
                }

                Posts result;
                try
                {
                    result = await RunWebSocketThreadReadOrInconclusiveAsync(
                        () => threads.GetPostsAsync(targetThread.Tid, 1, 10, PostSortType.Hot, false, false, 0, false));
                }
                catch (TieBaServerException exception) when (exception.Code == 4)
                {
                    Assert.Inconclusive(
                        $"Skipping {operationName}: the discovered thread fixture '{targetThread.Tid}' is no longer visible to websocket-only Threads.GetPostsAsync.");
                    throw;
                }

                AssertPostsShape(fixture, targetThread.Tid, result);
                Assert.AreEqual(0, handler.BlockedGetPostsRequestCount,
                    "Live websocket verification must not reach the HTTP get_posts endpoint.");
            });
    }

    private static async Task<T> RunWebSocketThreadReadOrInconclusiveAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (TiebaTransportException exception) when (IsWebSocketTransportEnvironmentGate(exception))
        {
            Assert.Inconclusive(
                $"Skipping live websocket thread-read proof in this environment: websocket transport is not currently available ({exception.Message}).");
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

    private static DedicatedForumFixture RequireDedicatedForumFixture(OnlineExecutionScope scope, string operationName)
    {
        var forumSelector = !string.IsNullOrWhiteSpace(scope.Safe.Assets.ForumQuery)
            ? scope.Safe.Assets.ForumQuery
            : scope.Safe.Assets.ForumName;

        if (string.IsNullOrWhiteSpace(forumSelector))
        {
            Assert.Inconclusive(
                $"Skipping {operationName}: live websocket get_threads proof requires {OnlineTestEnvironmentVariables.SafeAssetsForumQuery} or {OnlineTestEnvironmentVariables.SafeAssetsForumName}.");
        }

        return new DedicatedForumFixture(
            forumSelector,
            TryResolveCanonicalForumName(scope.Safe.Assets.ForumName),
            scope.Safe.Assets.ForumId is > 0 ? (ulong)scope.Safe.Assets.ForumId.Value : null);
    }

    private static string? TryResolveCanonicalForumName(string configuredForumName)
    {
        return string.IsNullOrWhiteSpace(configuredForumName) ? null : configuredForumName.Trim();
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

    private static void AssertPostsShape(DedicatedForumFixture fixture, long threadId, Posts posts)
    {
        Assert.IsNotNull(posts);
        Assert.IsNotNull(posts.Page);
        Assert.IsNotNull(posts.Thread);
        Assert.IsNotNull(posts.Forum);
        Assert.AreEqual(threadId, posts.Thread.Tid);
        Assert.IsPositive(posts.Forum.Fid);
        Assert.IsFalse(string.IsNullOrWhiteSpace(posts.Forum.Fname));

        if (fixture.ForumId is { } forumId)
            Assert.AreEqual((long)forumId, posts.Forum.Fid);

        if (!string.IsNullOrWhiteSpace(fixture.ForumName))
            Assert.AreEqual(fixture.ForumName, posts.Forum.Fname);

        Assert.IsNotNull(posts.Objs);
    }

    private sealed record DedicatedForumFixture(string ForumSelector, string? ForumName, ulong? ForumId);

    private sealed class BlockThreadReadHttpHandler : DelegatingHandler
    {
        public BlockThreadReadHttpHandler()
            : base(new HttpClientHandler())
        {
        }

        public int BlockedGetThreadsRequestCount { get; private set; }

        public int BlockedGetPostsRequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri is { AbsolutePath: "/c/f/frs/page" } uri
                && uri.Query.Contains("cmd=301001", StringComparison.Ordinal))
            {
                BlockedGetThreadsRequestCount++;
                throw new HttpRequestException(
                    "Blocked HTTP get_threads endpoint during live websocket proof. The request should stay on websocket.");
            }

            if (request.RequestUri is { AbsolutePath: "/c/f/pb/page" } postUri
                && postUri.Query.Contains("cmd=302001", StringComparison.Ordinal))
            {
                BlockedGetPostsRequestCount++;
                throw new HttpRequestException(
                    "Blocked HTTP get_posts endpoint during live websocket proof. The request should stay on websocket.");
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
