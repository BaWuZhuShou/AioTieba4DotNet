#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Api.AddBaWu;
using AioTieba4DotNet.Api.AddBawuBlacklist;
using AioTieba4DotNet.Api.DelBawuBlacklist;
using AioTieba4DotNet.Api.GetUnblockAppeals;
using AioTieba4DotNet.Api.HandleUnblockAppeals;
using AioTieba4DotNet.Api.Unblock;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.Admins;

[TestClass]
public sealed class AdminRemainingApiTests
{
    [TestMethod]
    public async Task AddBaWu_RequestAsync_PacksExpectedForm()
    {
        var httpCore = new RecordingHttpCore();
        var api = new AddBaWu(httpCore);

        var success = await api.RequestAsync(7356044, "target-user", "assist");

        Assert.IsTrue(success);
        Assert.AreEqual("/mo/q/bawuteamadd", httpCore.LastWebFormUri?.AbsolutePath);
        Assert.AreEqual("-", httpCore.GetWebFormValue("fn"));
        Assert.AreEqual("7356044", httpCore.GetWebFormValue("fid"));
        Assert.AreEqual("target-user", httpCore.GetWebFormValue("team_un"));
        Assert.AreEqual("assist", httpCore.GetWebFormValue("type"));
        Assert.AreEqual("tbs-123", httpCore.GetWebFormValue("tbs"));
    }

    [TestMethod]
    public async Task AddBawuBlacklist_RequestAsync_PacksExpectedHttpForm()
    {
        var httpCore = new RecordingHttpCore
        {
            WebFormResponse = """
                              {"errno":0,"errmsg":""}
                              """
        };
        var api = new AddBawuBlacklist(httpCore);

        var success = await api.RequestAsync("lol欧服", 42);

        Assert.IsTrue(success);
        Assert.AreEqual("http", httpCore.LastWebFormUri?.Scheme);
        Assert.AreEqual("/bawu2/platform/addBlack", httpCore.LastWebFormUri?.AbsolutePath);
        Assert.AreEqual("tbs-123", httpCore.GetWebFormValue("tbs"));
        Assert.AreEqual("42", httpCore.GetWebFormValue("user_id"));
        Assert.AreEqual("lol欧服", httpCore.GetWebFormValue("word"));
        Assert.AreEqual("utf-8", httpCore.GetWebFormValue("ie"));
    }

    [TestMethod]
    public async Task DelBawuBlacklist_RequestAsync_PacksExpectedHttpForm()
    {
        var httpCore = new RecordingHttpCore
        {
            WebFormResponse = """
                              {"errno":0,"errmsg":""}
                              """
        };
        var api = new DelBawuBlacklist(httpCore);

        var success = await api.RequestAsync("lol欧服", 42);

        Assert.IsTrue(success);
        Assert.AreEqual("http", httpCore.LastWebFormUri?.Scheme);
        Assert.AreEqual("/bawu2/platform/cancelBlack", httpCore.LastWebFormUri?.AbsolutePath);
        Assert.AreEqual("lol欧服", httpCore.GetWebFormValue("word"));
        Assert.AreEqual("tbs-123", httpCore.GetWebFormValue("tbs"));
        Assert.AreEqual("42", httpCore.GetWebFormValue("list[]"));
        Assert.AreEqual("utf-8", httpCore.GetWebFormValue("ie"));
    }

    [TestMethod]
    public async Task GetUnblockAppeals_RequestAsync_PacksFormAndParsesAppeals()
    {
        var httpCore = new RecordingHttpCore
        {
            WebFormResponse = """
                              {"no":0,"error":"","data":{"appeal_list":[{"appeal_id":"1001","appeal_reason":"reason","appeal_time":"1711700000","punish_reason":"spam","punish_start_time":"1711600000","punish_day_num":3,"operate_man":"moderator","user":{"id":42,"portrait":"tb.1.target?foo=bar","name":"target-user","name_show":"Target"}}],"has_more":1}}
                              """
        };
        var api = new GetUnblockAppeals(httpCore);

        var result = await api.RequestAsync(7356044, 2, 20);

        Assert.AreEqual("/mo/q/getBawuAppealList", httpCore.LastWebFormUri?.AbsolutePath);
        Assert.AreEqual("7356044", httpCore.GetWebFormValue("fid"));
        Assert.AreEqual("2", httpCore.GetWebFormValue("pn"));
        Assert.AreEqual("20", httpCore.GetWebFormValue("rn"));
        Assert.AreEqual("tbs-123", httpCore.GetWebFormValue("tbs"));
        Assert.AreEqual(1, result.Count);
        Assert.IsTrue(result.HasMore);
        Assert.AreEqual(1001L, result[0].AppealId);
        Assert.AreEqual("tb.1.target", result[0].Portrait);
        Assert.AreEqual("Target", result[0].NickName);
        Assert.AreEqual("moderator", result[0].OperatorName);
    }

    [TestMethod]
    public async Task HandleUnblockAppeals_RequestAsync_PacksIndexedAppealIdsAndStatus()
    {
        var httpCore = new RecordingHttpCore();
        var api = new HandleUnblockAppeals(httpCore);

        var success = await api.RequestAsync(7356044, new long[] { 1001, 1002 }, refuse: true);

        Assert.IsTrue(success);
        Assert.AreEqual("/mo/q/multiAppealhandle", httpCore.LastWebFormUri?.AbsolutePath);
        Assert.AreEqual("-", httpCore.GetWebFormValue("fn"));
        Assert.AreEqual("7356044", httpCore.GetWebFormValue("fid"));
        Assert.AreEqual("1001", httpCore.GetWebFormValue("appeal_list[0]"));
        Assert.AreEqual("1002", httpCore.GetWebFormValue("appeal_list[1]"));
        Assert.AreEqual("_", httpCore.GetWebFormValue("refuse_reason"));
        Assert.AreEqual("2", httpCore.GetWebFormValue("status"));
        Assert.AreEqual("tbs-123", httpCore.GetWebFormValue("tbs"));
    }

    [TestMethod]
    public async Task Unblock_RequestAsync_PacksExpectedForm()
    {
        var httpCore = new RecordingHttpCore();
        var api = new Unblock(httpCore);

        var success = await api.RequestAsync(7356044, 42);

        Assert.IsTrue(success);
        Assert.AreEqual("/mo/q/bawublockclear", httpCore.LastWebFormUri?.AbsolutePath);
        Assert.AreEqual("-", httpCore.GetWebFormValue("fn"));
        Assert.AreEqual("7356044", httpCore.GetWebFormValue("fid"));
        Assert.AreEqual("-", httpCore.GetWebFormValue("block_un"));
        Assert.AreEqual("42", httpCore.GetWebFormValue("block_uid"));
        Assert.AreEqual("tbs-123", httpCore.GetWebFormValue("tbs"));
    }

    private sealed class RecordingHttpCore : ITiebaHttpCore
    {
        public string WebFormResponse { get; init; } = """
                                                     {"no":0,"error":""}
                                                     """;

        public Account? Account { get; private set; } = new(new string('b', 192), new string('s', 64))
        {
            Tbs = "tbs-123"
        };

        public HttpClient HttpClient { get; } = new();

        public Uri? LastWebFormUri { get; private set; }
        public List<KeyValuePair<string, string>> LastWebFormData { get; private set; } = [];

        public void SetAccount(Account newAccount)
        {
            Account = newAccount;
        }

        public Task<string> SendAsync(Func<HttpRequestMessage> requestFactory, bool allowRetry = false,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<string> SendAppFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default)
        {
            LastWebFormUri = uri;
            LastWebFormData = [.. data];
            return Task.FromResult(WebFormResponse);
        }

        public string GetWebFormValue(string key) =>
            LastWebFormData.Last(entry => entry.Key == key).Value;
    }
}
