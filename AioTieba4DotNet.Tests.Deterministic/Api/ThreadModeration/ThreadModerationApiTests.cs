#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Api.Agree;
using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Api.DelPosts;
using AioTieba4DotNet.Api.DelThreads;
using AioTieba4DotNet.Api.Good;
using AioTieba4DotNet.Api.Move;
using AioTieba4DotNet.Api.Recommend;
using AioTieba4DotNet.Api.Recover;
using AioTieba4DotNet.Api.SetThreadPrivacy;
using AioTieba4DotNet.Api.Top;
using AioTieba4DotNet.Api.Ungood;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;
using AioTieba4DotNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.ThreadModeration;

[TestClass]
public class ThreadModerationApiTests
{
    [TestMethod]
    public async Task DelThread_RequestAsync_PacksBawuDeleteThreadForm()
    {
        var httpCore = new RecordingHttpCore();
        var api = new global::AioTieba4DotNet.Api.DelThread.DelThread(httpCore);

        var success = await api.RequestAsync(3581744, 10377929712, false);

        Assert.IsTrue(success);
        Assert.AreEqual("/c/c/bawu/delthread", httpCore.LastAppFormUri?.AbsolutePath);
        Assert.AreEqual("3581744", httpCore.GetAppFormValue("fid"));
        Assert.AreEqual("10377929712", httpCore.GetAppFormValue("z"));
        Assert.AreEqual("0", httpCore.GetAppFormValue("is_frs_mask"));
    }

    [TestMethod]
    public async Task DelPost_RequestAsync_PacksBawuDeletePostForm()
    {
        var httpCore = new RecordingHttpCore();
        var api = new global::AioTieba4DotNet.Api.DelPost.DelPost(httpCore);

        var success = await api.RequestAsync(3581744, 10377929712, 153071185710);

        Assert.IsTrue(success);
        Assert.AreEqual("/c/c/bawu/delpost", httpCore.LastAppFormUri?.AbsolutePath);
        Assert.AreEqual("3581744", httpCore.GetAppFormValue("fid"));
        Assert.AreEqual("153071185710", httpCore.GetAppFormValue("pid"));
        Assert.AreEqual("10377929712", httpCore.GetAppFormValue("z"));
    }

    [TestMethod]
    public async Task BatchDeleteApis_PackJoinedIdsAndBlockMode()
    {
        var httpCore = new RecordingHttpCore();

        var deleteThreads = new DelThreads(httpCore);
        var deletePosts = new DelPosts(httpCore);

        await deleteThreads.RequestAsync(3581744, new long[] { 1, 2, 3 }, true);
        Assert.AreEqual("/c/c/bawu/multiDelThread", httpCore.LastAppFormUri?.AbsolutePath);
        Assert.AreEqual("1,2,3", httpCore.GetAppFormValue("thread_ids"));
        Assert.AreEqual("2", httpCore.GetAppFormValue("type"));

        await deletePosts.RequestAsync(3581744, 99, new long[] { 11, 22 }, false);
        Assert.AreEqual("/c/c/bawu/multiDelPost", httpCore.LastAppFormUri?.AbsolutePath);
        Assert.AreEqual("11,22", httpCore.GetAppFormValue("post_ids"));
        Assert.AreEqual("99", httpCore.GetAppFormValue("thread_id"));
        Assert.AreEqual("1", httpCore.GetAppFormValue("type"));
    }

    [TestMethod]
    public async Task GoodAndUngood_RequestAsync_PackExpectedModerationFields()
    {
        var httpCore = new RecordingHttpCore();

        var good = new Good(httpCore);
        var ungood = new Ungood(httpCore);

        await good.RequestAsync("lol欧服", 3581744, 10377929712, 88);
        Assert.AreEqual("/c/c/bawu/commitgood", httpCore.LastAppFormUri?.AbsolutePath);
        Assert.AreEqual("88", httpCore.GetAppFormValue("cid"));
        Assert.AreEqual("set", httpCore.GetAppFormValue("ntn"));
        Assert.AreEqual("lol欧服", httpCore.GetAppFormValue("word"));

        await ungood.RequestAsync("lol欧服", 3581744, 10377929712);
        Assert.AreEqual("/c/c/bawu/commitgood", httpCore.LastAppFormUri?.AbsolutePath);
        Assert.IsNull(httpCore.GetOptionalAppFormValue("cid"));
        Assert.AreEqual("lol欧服", httpCore.GetAppFormValue("word"));
    }

    [TestMethod]
    public async Task TopAndMove_RequestAsync_PackExpectedPayloads()
    {
        var httpCore = new RecordingHttpCore();

        var top = new Top(httpCore);
        var move = new Move(httpCore);

        await top.RequestAsync("lol欧服", 3581744, 10377929712, true, false);
        Assert.AreEqual("/c/c/bawu/committop", httpCore.LastAppFormUri?.AbsolutePath);
        Assert.AreEqual("1", httpCore.GetAppFormValue("is_member_top"));
        Assert.AreEqual(string.Empty, httpCore.GetAppFormValue("ntn"));

        await move.RequestAsync(3581744, 10377929712, 202, 101);
        Assert.AreEqual("/c/c/bawu/moveTabThread", httpCore.LastAppFormUri?.AbsolutePath);
        Assert.AreEqual("[{\"thread_id\":10377929712,\"from_tab_id\":101,\"to_tab_id\":202}]",
            httpCore.GetAppFormValue("threads"));
    }

    [TestMethod]
    public async Task Recommend_RequestAsync_ThrowsWhenPushFails()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse =
                "{\"error_code\":0,\"error_msg\":\"\",\"data\":{\"is_push_success\":0,\"msg\":\"denied\"}}"
        };
        var api = new Recommend(httpCore);

        var exception = await ThrowsAsync<TieBaServerException>(() => api.RequestAsync(3581744, 10377929712));

        Assert.IsTrue(exception.Message.Contains("denied", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task Recommend_RequestAsync_UsesDefaultMessageWhenMsgIsMissing()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = "{\"error_code\":0,\"error_msg\":\"\",\"data\":{\"is_push_success\":0}}"
        };
        var api = new Recommend(httpCore);

        var exception = await ThrowsAsync<TieBaServerException>(() => api.RequestAsync(3581744, 10377929712));

        Assert.IsTrue(exception.Message.Contains("Recommend failed.", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task Recommend_RequestAsync_ReturnsTrueWhenPushSucceeds()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse =
                "{\"error_code\":0,\"error_msg\":\"\",\"data\":{\"is_push_success\":1,\"msg\":\"ok\"}}"
        };
        var api = new Recommend(httpCore);

        var result = await api.RequestAsync(3581744, 10377929712);

        Assert.IsTrue(result);
        Assert.AreEqual("3581744", httpCore.GetAppFormValue("forum_id"));
        Assert.AreEqual("10377929712", httpCore.GetAppFormValue("thread_id"));
    }

    [TestMethod]
    public async Task RecoverAndSetThreadPrivacy_RequestAsync_PackOwnedTargetFields()
    {
        var httpCore = new RecordingHttpCore { WebFormResponse = "{\"no\":0,\"error\":\"\"}" };

        var recover = new Recover(httpCore);
        var privacy = new SetThreadPrivacy(httpCore);

        await recover.RequestAsync(3581744, 0, 153071185710, false);
        Assert.AreEqual("/mo/q/bawurecoverthread", httpCore.LastWebFormUri?.AbsolutePath);
        Assert.AreEqual("3581744", httpCore.GetWebFormValue("fid"));
        Assert.AreEqual("153071185710", httpCore.GetWebFormValue("pid_list[]"));
        Assert.AreEqual("1", httpCore.GetWebFormValue("type_list[]"));

        await privacy.RequestAsync(3581744, 10377929712, 153071185710, true);
        Assert.AreEqual("/c/c/thread/setPrivacy", httpCore.LastAppFormUri?.AbsolutePath);
        Assert.AreEqual("1", httpCore.GetAppFormValue("is_hide"));
        Assert.AreEqual("153071185710", httpCore.GetAppFormValue("post_id"));
        Assert.AreEqual("10377929712", httpCore.GetAppFormValue("thread_id"));
    }

    [TestMethod]
    public async Task Agree_RequestAsync_PacksExpectedObjectTypeForThreadPostAndComment()
    {
        var httpCore = new RecordingHttpCore();
        var api = new global::AioTieba4DotNet.Api.Agree.Agree(httpCore);

        await api.RequestAsync(10377929712, 0, false, false, false);
        Assert.AreEqual("3", httpCore.GetAppFormValue("obj_type"));

        await api.RequestAsync(10377929712, 153071185710, false, false, false);
        Assert.AreEqual("1", httpCore.GetAppFormValue("obj_type"));

        await api.RequestAsync(10377929712, 153071185710, true, false, false);
        Assert.AreEqual("2", httpCore.GetAppFormValue("obj_type"));
    }

    private sealed class RecordingHttpCore : ITiebaHttpCore
    {
        public string AppFormResponse { get; init; } = "{\"error_code\":0,\"error_msg\":\"\"}";
        public string WebFormResponse { get; init; } = "{\"error_code\":0,\"error_msg\":\"\"}";

        public Account? Account { get; private set; } = new(new string('a', 192), new string('b', 64)) { Tbs = "tbs" };

        public HttpClient HttpClient { get; } = new();

        public Uri? LastAppFormUri { get; private set; }
        public List<KeyValuePair<string, string>> LastAppFormData { get; private set; } = [];
        public Uri? LastWebFormUri { get; private set; }
        public List<KeyValuePair<string, string>> LastWebFormData { get; private set; } = [];

        public void SetAccount(Account newAccount)
        {
            Account = newAccount;
        }

        public Task<string> SendAsync(Func<HttpRequestMessage> requestFactory, bool allowRetry = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> SendAppFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default)
        {
            LastAppFormUri = uri;
            LastAppFormData = [.. data];
            return Task.FromResult(AppFormResponse);
        }

        public Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default)
        {
            LastWebFormUri = uri;
            LastWebFormData = [.. data];
            return Task.FromResult(WebFormResponse);
        }

        public string GetAppFormValue(string key)
        {
            return LastAppFormData.Last(entry => entry.Key == key).Value;
        }

        public string? GetOptionalAppFormValue(string key)
        {
            return LastAppFormData.LastOrDefault(entry => entry.Key == key).Value;
        }

        public string GetWebFormValue(string key)
        {
            return LastWebFormData.Last(entry => entry.Key == key).Value;
        }
    }

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
}
