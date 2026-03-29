#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Protocols;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Protocols;

[TestClass]
public class LegacyThreadWriteProtocolTests
{
    [TestMethod]
    public async Task DelPostsAsync_RejectsOversizedBatchBeforeForumLookup()
    {
        var forum = new CountingForumProtocol();
        var httpCore = new RecordingHttpCore();
        var protocol = CreateProtocol(CreateAuthenticatedSession(httpCore), forum);
        IReadOnlyList<long> pids = Enumerable.Range(1, 31).Select(value => (long)value).ToArray();

        await ThrowsAsync<ArgumentOutOfRangeException>(() => protocol.DelPostsAsync("lol欧服", 10377929712, pids, block: false));

        Assert.AreEqual(0, forum.GetFidCalls);
    }

    [TestMethod]
    public async Task RecommendAsync_RequiresAuthenticatedAccountBeforeForumLookup()
    {
        var httpCore = new RecordingHttpCore();
        var forum = new CountingForumProtocol();
        var protocol = CreateProtocol(CreateGuestSession(httpCore), forum);

        await ThrowsAsync<TiebaAuthenticationException>(() => protocol.RecommendAsync("lol欧服", 10377929712));

        Assert.AreEqual(0, forum.GetFidCalls);
        Assert.IsEmpty(httpCore.AppFormRequests);
    }

    [TestMethod]
    public async Task GoodAsync_ResolvesCategoryBeforeCommit()
    {
        var httpCore = new RecordingHttpCore();
        httpCore.AppFormResponses["/c/c/bawu/goodlist"] =
            "{\"error_code\":0,\"error_msg\":\"\",\"cates\":[{\"class_name\":\"精选\",\"class_id\":88}]}";
        httpCore.AppFormResponses["/c/c/bawu/commitgood"] = "{\"error_code\":0,\"error_msg\":\"\"}";
        var forum = new CountingForumProtocol();
        var protocol = CreateProtocol(CreateAuthenticatedSession(httpCore), forum);

        var success = await protocol.GoodAsync("lol欧服", 10377929712, "精选");

        Assert.IsTrue(success);
        Assert.AreEqual(1, forum.GetFidCalls);
        Assert.HasCount(2, httpCore.AppFormRequests);
        Assert.AreEqual("/c/c/bawu/goodlist", httpCore.AppFormRequests[0].Uri.AbsolutePath);
        Assert.AreEqual("/c/c/bawu/commitgood", httpCore.AppFormRequests[1].Uri.AbsolutePath);
        Assert.AreEqual("88", GetValue(httpCore.AppFormRequests[1].Data, "cid"));
        Assert.AreEqual("tbs", GetValue(httpCore.AppFormRequests[1].Data, "tbs"));
    }

    [TestMethod]
    public async Task RecoverAsync_UsesWebFormForPostRecovery()
    {
        var httpCore = new RecordingHttpCore
        {
            WebFormResponse = "{\"no\":0,\"error\":\"\"}"
        };
        var forum = new CountingForumProtocol();
        var protocol = CreateProtocol(CreateAuthenticatedSession(httpCore), forum);

        var success = await protocol.RecoverAsync("lol欧服", tid: 0, pid: 153071185710, isHide: false);

        Assert.IsTrue(success);
        Assert.AreEqual(1, forum.GetFidCalls);
        Assert.HasCount(1, httpCore.WebFormRequests);
        Assert.AreEqual("/mo/q/bawurecoverthread", httpCore.WebFormRequests[0].Uri.AbsolutePath);
        Assert.AreEqual("153071185710", GetValue(httpCore.WebFormRequests[0].Data, "pid_list[]"));
        Assert.AreEqual("1", GetValue(httpCore.WebFormRequests[0].Data, "type_list[]"));
    }

    private static LegacyThreadProtocol CreateProtocol(TiebaClientSession session, CountingForumProtocol forumProtocol)
    {
        return new LegacyThreadProtocol(
            new LegacyTransportContext(session),
            forumProtocol);
    }

    private static TiebaClientSession CreateAuthenticatedSession(RecordingHttpCore httpCore)
    {
        return new TiebaClientSession(
            new TiebaOptions
            {
                Bduss = new string('a', 192),
                Stoken = new string('b', 64),
                TransportMode = TiebaTransportMode.Http
            },
            httpCore,
            new StubWsCore(),
            _ => Task.FromResult("tbs"));
    }

    private static TiebaClientSession CreateGuestSession(RecordingHttpCore httpCore)
    {
        return new TiebaClientSession(
            new TiebaOptions
            {
                TransportMode = TiebaTransportMode.Http
            },
            httpCore,
            new StubWsCore(),
            _ => Task.FromResult("tbs"));
    }

    private static string GetValue(IReadOnlyList<KeyValuePair<string, string>> data, string key) =>
        data.Last(entry => entry.Key == key).Value;

    private static async Task<TException> ThrowsAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException exception)
        {
            return exception;
        }

        Assert.Fail($"Expected exception of type {typeof(TException).Name} was not thrown.");
        throw new InvalidOperationException();
    }

    private sealed class RecordingHttpCore : ITiebaHttpCore
    {
        public Dictionary<string, string> AppFormResponses { get; } = [];

        public string AppFormResponse { get; init; } = "{\"error_code\":0,\"error_msg\":\"\"}";

        public string WebFormResponse { get; init; } = "{\"error_code\":0,\"error_msg\":\"\"}";

        public Account? Account { get; private set; }

        public HttpClient HttpClient { get; } = new();

        public List<(Uri Uri, List<KeyValuePair<string, string>> Data)> AppFormRequests { get; } = [];

        public List<(Uri Uri, List<KeyValuePair<string, string>> Data)> WebFormRequests { get; } = [];

        public void SetAccount(Account newAccount)
        {
            Account = newAccount;
        }

        public Task<string> SendAsync(Func<HttpRequestMessage> requestFactory, bool allowRetry = false,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<string> SendAppFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default)
        {
            AppFormRequests.Add((uri, [.. data]));
            return Task.FromResult(AppFormResponses.TryGetValue(uri.AbsolutePath, out var response)
                ? response
                : AppFormResponse);
        }

        public Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default)
        {
            WebFormRequests.Add((uri, [.. data]));
            return Task.FromResult(WebFormResponse);
        }
    }

    private sealed class StubWsCore : ITiebaWsCore
    {
        public Account? Account { get; private set; }

        public void SetAccount(Account newAccount)
        {
            Account = newAccount;
        }

        public Task ConnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SendAsync(WSReq req, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<WSRes> SendAsync(int cmd, byte[] data, bool encrypt = true,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public IAsyncEnumerable<WSRes> ListenAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task CloseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class CountingForumProtocol : IForumProtocol
    {
        public int GetFidCalls { get; private set; }

        public Task<ulong> GetFidAsync(string fname, CancellationToken cancellationToken = default)
        {
            GetFidCalls++;
            return Task.FromResult(3581744UL);
        }

        public Task<string> GetFnameAsync(ulong fid, CancellationToken cancellationToken = default) =>
            Task.FromResult("lol欧服");

        public Task<ForumDetail> GetDetailAsync(ulong fid, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<ForumDetail> GetDetailAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> LikeAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> UnlikeAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> SignAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<global::AioTieba4DotNet.Models.Forums.Forum> GetForumAsync(string fname,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<bool> DelBaWuAsync(string fname, string portrait, string baWuType,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
