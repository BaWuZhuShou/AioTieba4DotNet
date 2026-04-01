#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Api.AddBlacklistOld;
using AioTieba4DotNet.Api.DelBlacklistOld;
using AioTieba4DotNet.Api.GetBawuPerm;
using AioTieba4DotNet.Api.LikeForum;
using AioTieba4DotNet.Api.Login;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Models.Admins;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api;

[TestClass]
public sealed class CompatibilityWrapperApiTests
{
    [TestMethod]
    public async Task AddBlacklistOld_RequestAsync_CoversEmptyBdussAndErrornoBranches()
    {
        var successCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"error_msg":"","errorno":0,"errmsg":""}
                              """
        };
        var successApi = new AddBlacklistOld(successCore);

        var success = await successApi.RequestAsync(42);

        Assert.IsTrue(success);
        Assert.AreEqual("http", successCore.LastAppFormUri?.Scheme);
        Assert.AreEqual("/c/c/user/userMuteAdd", successCore.LastAppFormUri?.AbsolutePath);
        Assert.AreEqual(string.Empty, successCore.GetAppFormValue("BDUSS"));
        Assert.AreEqual("42", successCore.GetAppFormValue("mute_user"));

        var failureCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"error_msg":"","errorno":9,"errmsg":"denied"}
                              """
        };
        failureCore.SetAccount(new Account(new string('b', 192), new string('s', 64)));
        var failureApi = new AddBlacklistOld(failureCore);

        var exception = await ThrowsAsync<TieBaServerException>(() => failureApi.RequestAsync(99));

        Assert.AreEqual(9, exception.Code);
        StringAssert.Contains(exception.Message, "denied");
    }

    [TestMethod]
    public async Task DelBlacklistOld_RequestAsync_CoversRequestShapeAndErrorCodeBranch()
    {
        var successCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"error_msg":"","errorno":0,"errmsg":""}
                              """
        };
        successCore.SetAccount(new Account(new string('b', 192), new string('s', 64)));
        var successApi = new DelBlacklistOld(successCore);

        var success = await successApi.RequestAsync(123);

        Assert.IsTrue(success);
        Assert.AreEqual("http", successCore.LastAppFormUri?.Scheme);
        Assert.AreEqual("/c/c/user/userMuteDel", successCore.LastAppFormUri?.AbsolutePath);
        Assert.AreEqual("123", successCore.GetAppFormValue("mute_user"));
        Assert.AreEqual(successCore.Account!.Bduss, successCore.GetAppFormValue("BDUSS"));

        var failureCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":7,"error_msg":"blocked","errorno":0,"errmsg":""}
                              """
        };
        failureCore.SetAccount(new Account(new string('b', 192), new string('s', 64)));
        var failureApi = new DelBlacklistOld(failureCore);

        var exception = await ThrowsAsync<TieBaServerException>(() => failureApi.RequestAsync(321));

        Assert.AreEqual(7, exception.Code);
        StringAssert.Contains(exception.Message, "blocked");
    }

    [TestMethod]
    public async Task GetBawuPerm_RequestAsync_CoversPermissionParsingAndFallbackData()
    {
        var httpCore = new RecordingHttpCore
        {
            WebGetResponses =
            {
                ["/mo/q/getAuthToolPerm"] = new Queue<string>(new[]
                {
                    """
                    {"no":0,"error":"","data":{"perm_setting":{"category_user":[{"switch":true,"perm":2},{"switch":0,"perm":4}],"category_thread":[{"switch":"1","perm":5},{"switch":null,"perm":3}]}}}
                    """,
                    """
                    {"no":0,"error":"","data":null}
                    """
                })
            }
        };
        var api = new GetBawuPerm(httpCore);

        var permissions = await api.RequestAsync(7356044, "tb.1.target");
        var fallback = await api.RequestAsync(7356044, "tb.1.target");

        Assert.AreEqual("https", httpCore.LastWebGetUri?.Scheme);
        Assert.AreEqual("/mo/q/getAuthToolPerm", httpCore.LastWebGetUri?.AbsolutePath);
        Assert.AreEqual("7356044", httpCore.GetWebGetValue("forum_id"));
        Assert.AreEqual("tb.1.target", httpCore.GetWebGetValue("portrait"));
        Assert.AreEqual(BawuPermType.RecoverAppeal | BawuPermType.UnblockAppeal, permissions.Permissions);
        Assert.AreEqual(BawuPermType.None, fallback.Permissions);
    }

    [TestMethod]
    public async Task LikeForum_RequestAsync_CoversEmbeddedErrorBranchesAndPayload()
    {
        var successCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"error_msg":""}
                              """
        };
        successCore.SetAccount(new Account(new string('b', 192), new string('s', 64)) { Tbs = "tbs-123" });
        var successApi = new LikeForum(successCore);

        var success = await successApi.RequestAsync(7356044);

        Assert.IsTrue(success);
        Assert.AreEqual("/c/c/forum/like", successCore.LastAppFormUri?.AbsolutePath);
        Assert.AreEqual(successCore.Account!.Bduss, successCore.GetAppFormValue("BDUSS"));
        Assert.AreEqual("7356044", successCore.GetAppFormValue("fid"));
        Assert.AreEqual("tbs-123", successCore.GetAppFormValue("tbs"));

        var failureCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"error_msg":"","error":{"errno":325,"errmsg":"denied"}}
                              """
        };
        failureCore.SetAccount(new Account(new string('b', 192), new string('s', 64)) { Tbs = "tbs-456" });
        var failureApi = new LikeForum(failureCore);

        var exception = await ThrowsAsync<TieBaServerException>(() => failureApi.RequestAsync(7356044));

        Assert.AreEqual(325, exception.Code);
        StringAssert.Contains(exception.Message, "denied");
    }

    [TestMethod]
    public async Task LikeForum_RequestAsync_UsesEmptyFallbackMessageWhenErrmsgMissing()
    {
        var httpCore = new RecordingHttpCore
        {
            AppFormResponse = """
                               {"error_code":0,"error_msg":"","error":{"errno":325}}
                               """
        };
        httpCore.SetAccount(new Account(new string('b', 192), new string('s', 64)) { Tbs = "tbs-789" });
        var api = new LikeForum(httpCore);

        var exception = await ThrowsAsync<TieBaServerException>(() => api.RequestAsync(7356044));

        Assert.AreEqual(325, exception.Code);
        StringAssert.Contains(exception.Message, "325");
    }

    [TestMethod]
    public async Task Login_RequestAsync_CoversSuccessAndMissingAntiFailureBranches()
    {
        var successCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"error_msg":"","user":{"id":42,"portrait":"tb.1.login?012345678901","name":"login-user"},"anti":{"tbs":"tbs-123"}}
                              """
        };
        successCore.SetAccount(new Account(new string('b', 192), new string('s', 64)));
        var successApi = new Login(successCore);

        var (user, tbs) = await successApi.RequestAsync();

        Assert.AreEqual("/c/s/login", successCore.LastAppFormUri?.AbsolutePath);
        Assert.AreEqual(Const.MainVersion, successCore.GetAppFormValue("_client_version"));
        Assert.AreEqual(successCore.Account!.Bduss, successCore.GetAppFormValue("bdusstoken"));
        Assert.AreEqual(42L, user.UserId);
        Assert.AreEqual("tb.1.login", user.Portrait);
        Assert.AreEqual("login-user", user.UserName);
        Assert.AreEqual("tbs-123", tbs);

        var failureCore = new RecordingHttpCore
        {
            AppFormResponse = """
                              {"error_code":0,"error_msg":"","user":{"id":43,"portrait":"tb.1.login","name":"login-user"},"anti":{}}
                              """
        };
        failureCore.SetAccount(new Account(new string('b', 192), new string('s', 64)));
        var failureApi = new Login(failureCore);

        await ThrowsAsync<NullReferenceException>(() => failureApi.RequestAsync());
    }

    private sealed class RecordingHttpCore : ITiebaHttpCore
    {
        public Account? Account { get; private set; }

        public HttpClient HttpClient { get; } = new();

        public string AppFormResponse { get; init; } = """
                                                 {"error_code":0,"error_msg":""}
                                                 """;

        public Dictionary<string, Queue<string>> WebGetResponses { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Uri? LastAppFormUri { get; private set; }

        public List<KeyValuePair<string, string>> LastAppFormData { get; private set; } = [];

        public Uri? LastWebGetUri { get; private set; }

        public List<KeyValuePair<string, string>> LastWebGetParameters { get; private set; } = [];

        public void SetAccount(Account newAccount) => Account = newAccount;

        public Task<string> SendAsync(Func<HttpRequestMessage> requestFactory, bool allowRetry = false,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<string> SendAppFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default)
        {
            LastAppFormUri = uri;
            LastAppFormData = [.. data];
            return Task.FromResult(AppFormResponse);
        }

        public Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters,
            CancellationToken cancellationToken = default)
        {
            LastWebGetUri = uri;
            LastWebGetParameters = [.. parameters];
            if (!WebGetResponses.TryGetValue(uri.AbsolutePath, out var queue) || queue.Count == 0)
                throw new InvalidOperationException($"No queued web response for '{uri.AbsolutePath}'.");

            return Task.FromResult(queue.Dequeue());
        }

        public Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public string GetAppFormValue(string key) => LastAppFormData.Last(entry => entry.Key == key).Value;

        public string GetWebGetValue(string key) => LastWebGetParameters.Last(entry => entry.Key == key).Value;
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
