#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Api.GetBawuBlacklist;
using AioTieba4DotNet.Api.GetBawuPerm;
using AioTieba4DotNet.Api.GetBawuPostlogs;
using AioTieba4DotNet.Api.GetBawuUserlogs;
using AioTieba4DotNet.Api.GetBlocks;
using AioTieba4DotNet.Api.SetBawuPerm;
using AioTieba4DotNet.Models.Admins;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.Admins;

[TestClass]
public sealed class AdminReadConfigApiTests
{
    [TestMethod]
    public async Task GetBawuBlacklist_RequestAsync_PacksQueryAndParsesUsers()
    {
        var httpCore = new RecordingHttpCore
        {
            WebGetResponse = CreateBlacklistHtml()
        };
        var api = new GetBawuBlacklist(httpCore);

        var result = await api.RequestAsync("lol欧服", 2);

        Assert.AreEqual("/bawu2/platform/listBlackUser", httpCore.LastWebGetUri?.AbsolutePath);
        Assert.AreEqual("lol欧服", httpCore.GetWebGetValue("word"));
        Assert.AreEqual("2", httpCore.GetWebGetValue("pn"));
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(42L, result[0].UserId);
        Assert.AreEqual("target-user", result[0].UserName);
        Assert.AreEqual("tb.1.target", result[0].Portrait);
        Assert.AreEqual(2, result.Page.CurrentPage);
        Assert.AreEqual(5, result.Page.TotalPage);
        Assert.IsTrue(result.HasMore);
    }

    [TestMethod]
    public async Task GetBawuPerm_RequestAsync_PacksQueryAndParsesPermissionFlags()
    {
        var httpCore = new RecordingHttpCore
        {
            WebGetResponse = """
                             {"no":0,"error":"","data":{"perm_setting":{"category_user":[{"switch":1,"perm":4},{"switch":"1","perm":5}],"category_thread":[{"switch":true,"perm":3},{"switch":0,"perm":2}]}}}
                             """
        };
        var api = new GetBawuPerm(httpCore);

        var result = await api.RequestAsync(7356044, "tb.1.target");

        Assert.AreEqual("/mo/q/getAuthToolPerm", httpCore.LastWebGetUri?.AbsolutePath);
        Assert.AreEqual("7356044", httpCore.GetWebGetValue("forum_id"));
        Assert.AreEqual("tb.1.target", httpCore.GetWebGetValue("portrait"));
        Assert.AreEqual(BawuPermType.Unblock | BawuPermType.UnblockAppeal | BawuPermType.Recover,
            result.Permissions);
    }

    [TestMethod]
    public async Task SetBawuPerm_RequestAsync_PacksPermissionSettingsInUpstreamOrder()
    {
        var httpCore = new RecordingHttpCore
        {
            WebFormResponse = """
                              {"no":0,"error":""}
                              """
        };
        var api = new SetBawuPerm(httpCore);

        var success = await api.RequestAsync(7356044, "tb.1.target",
            BawuPermType.Unblock | BawuPermType.UnblockAppeal | BawuPermType.RecoverAppeal);

        Assert.IsTrue(success);
        Assert.AreEqual("/mo/q/setAuthToolPerm", httpCore.LastWebFormUri?.AbsolutePath);
        Assert.AreEqual("7356044", httpCore.GetWebFormValue("forum_id"));
        Assert.AreEqual("tb.1.target", httpCore.GetWebFormValue("auth_user_portrait"));
        Assert.AreEqual(
            "[{\"switch\":1,\"perm\":4},{\"switch\":1,\"perm\":5},{\"switch\":0,\"perm\":3},{\"switch\":1,\"perm\":2}]",
            httpCore.GetWebFormValue("perm_setting"));
    }

    [TestMethod]
    public async Task GetBawuPostlogs_RequestAsync_PacksFiltersAndParsesReplyLog()
    {
        var httpCore = new RecordingHttpCore
        {
            WebGetResponse = CreatePostLogsHtml()
        };
        var api = new GetBawuPostlogs(httpCore);
        var start = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 3, 2, 0, 0, 0, TimeSpan.Zero);

        var result = await api.RequestAsync("lol欧服", 3, "operator-user", BawuSearchType.Operator, start, end, 9);

        Assert.AreEqual("/bawu2/platform/listPostLog", httpCore.LastWebGetUri?.AbsolutePath);
        Assert.AreEqual("lol欧服", httpCore.GetWebGetValue("word"));
        Assert.AreEqual("3", httpCore.GetWebGetValue("pn"));
        Assert.AreEqual("utf-8", httpCore.GetWebGetValue("ie"));
        Assert.AreEqual("9", httpCore.GetWebGetValue("op_type"));
        Assert.AreEqual("operator-user", httpCore.GetWebGetValue("svalue"));
        Assert.AreEqual("op_uname", httpCore.GetWebGetValue("stype"));
        Assert.AreEqual(start.ToUnixTimeSeconds().ToString(), httpCore.GetWebGetValue("begin"));
        Assert.AreEqual(end.ToUnixTimeSeconds().ToString(), httpCore.GetWebGetValue("end"));
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(123L, result[0].Tid);
        Assert.AreEqual(456L, result[0].Pid);
        Assert.AreEqual("原帖标题", result[0].Title);
        Assert.AreEqual("正文", result[0].Text);
        Assert.AreEqual("tb.1.poster", result[0].PostPortrait);
        Assert.AreEqual("operator-user", result[0].OperatorUserName);
        Assert.AreEqual(1, result[0].Medias.Count);
        Assert.AreEqual("hash", result[0].Medias[0].Hash);
    }

    [TestMethod]
    public async Task GetBawuUserlogs_RequestAsync_PacksUserSearchAndParsesDuration()
    {
        var httpCore = new RecordingHttpCore
        {
            WebGetResponse = CreateUserLogsHtml()
        };
        var api = new GetBawuUserlogs(httpCore);

        var result = await api.RequestAsync("lol欧服", 1, "target-user", BawuSearchType.User, null, null, 0);

        Assert.AreEqual("/bawu2/platform/listUserLog", httpCore.LastWebGetUri?.AbsolutePath);
        Assert.AreEqual("target-user", httpCore.GetWebGetValue("svalue"));
        Assert.AreEqual("post_uname", httpCore.GetWebGetValue("stype"));
        Assert.IsNull(httpCore.GetOptionalWebGetValue("op_type"));
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("tb.1.target", result[0].UserPortrait);
        Assert.AreEqual("封禁", result[0].OperationType);
        Assert.AreEqual(7, result[0].OperationDurationDays);
        Assert.AreEqual("operator-user", result[0].OperatorUserName);
    }

    [TestMethod]
    public async Task GetBlocks_RequestAsync_PacksQueryAndParsesBlocks()
    {
        var httpCore = new RecordingHttpCore
        {
            WebGetResponse = """
                             {"no":0,"error":"","data":{"content":"<li><a attr-uid=\"42\" attr-un=\"target-user\" attr-nn=\"Target\" attr-blockday=\"3\"></a></li>","page":{"size":20,"pn":2,"total_page":5,"total_count":91,"have_next":1}}}
                             """
        };
        var api = new GetBlocks(httpCore);

        var result = await api.RequestAsync(7356044, "target-user", 2);

        Assert.AreEqual("/mo/q/bawublock", httpCore.LastWebGetUri?.AbsolutePath);
        Assert.AreEqual("-", httpCore.GetWebGetValue("fn"));
        Assert.AreEqual("7356044", httpCore.GetWebGetValue("fid"));
        Assert.AreEqual("target-user", httpCore.GetWebGetValue("word"));
        Assert.AreEqual("1", httpCore.GetWebGetValue("is_ajax"));
        Assert.AreEqual("2", httpCore.GetWebGetValue("pn"));
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(42L, result[0].UserId);
        Assert.AreEqual("target-user", result[0].UserName);
        Assert.AreEqual("Target", result[0].NickNameOld);
        Assert.AreEqual(3, result[0].Day);
        Assert.AreEqual(20, result.Page.PageSize);
        Assert.IsTrue(result.Page.HasMore);
        Assert.IsTrue(result.Page.HasPrevious);
    }

    private static string CreateBlacklistHtml() => """
                                                   <div class="breadcrumbs"><em>3</em></div>
                                                   <div class="tbui_pagination"><ul><li class="active">2</li><li>(5)</li></ul></div>
                                                   <table><tbody>
                                                   <tr>
                                                     <input data-user-name="target-user" data-user-id="42" />
                                                     <td class="left_cell"><a href="/home/main?id=tb.1.target#/"></a></td>
                                                   </tr>
                                                   </tbody></table>
                                                   """;

    private static string CreatePostLogsHtml() => """
                                                  <div class="breadcrumbs"><em>8</em></div>
                                                  <div class="tbui_pagination"><ul><li class="active">1</li><li>(4)</li></ul></div>
                                                  <table><tbody>
                                                  <tr>
                                                    <td>
                                                      <div class="post_meta">
                                                        <div><a href="/home/main?id=tb.1.poster#/">poster</a></div>
                                                        <time>03-04 05:06</time>
                                                      </div>
                                                      <div class="post_content">
                                                        <h1><a href="/p/123?see_lz=1#456" title="回复：原帖标题">回复：原帖标题</a></h1>
                                                        <div>abcdefghijkl正文</div>
                                                        <div><a href="https://img-origin"><img original="https://img-src/hash.jpg" /></a></div>
                                                      </div>
                                                    </td>
                                                    <td>删帖</td>
                                                    <td>operator-user</td>
                                                    <td>2026-03-30 11:22</td>
                                                  </tr>
                                                  </tbody></table>
                                                  """;

    private static string CreateUserLogsHtml() => """
                                                  <div class="breadcrumbs"><em>2</em></div>
                                                  <div class="tbui_pagination"><ul><li class="active">1</li><li>(1)</li></ul></div>
                                                  <table><tbody>
                                                  <tr>
                                                    <td><a href="/home/main?id=tb.1.target#/">target</a></td>
                                                    <td>封禁</td>
                                                    <td>7 天</td>
                                                    <td>operator-user</td>
                                                    <td>2026-03-30 11:22</td>
                                                  </tr>
                                                  </tbody></table>
                                                  """;

    private sealed class RecordingHttpCore : ITiebaHttpCore
    {
        public string WebGetResponse { get; init; } = string.Empty;
        public string WebFormResponse { get; init; } = """
                                                     {"no":0,"error":""}
                                                     """;

        public Account? Account { get; private set; } = new();

        public HttpClient HttpClient { get; } = new();

        public Uri? LastWebGetUri { get; private set; }
        public List<KeyValuePair<string, string>> LastWebGetParameters { get; private set; } = [];
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
            CancellationToken cancellationToken = default)
        {
            LastWebGetUri = uri;
            LastWebGetParameters = [.. parameters];
            return Task.FromResult(WebGetResponse);
        }

        public Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default)
        {
            LastWebFormUri = uri;
            LastWebFormData = [.. data];
            return Task.FromResult(WebFormResponse);
        }

        public string GetWebGetValue(string key) =>
            LastWebGetParameters.Last(entry => entry.Key == key).Value;

        public string? GetOptionalWebGetValue(string key) =>
            LastWebGetParameters.LastOrDefault(entry => entry.Key == key).Value;

        public string GetWebFormValue(string key) =>
            LastWebFormData.Last(entry => entry.Key == key).Value;
    }
}
