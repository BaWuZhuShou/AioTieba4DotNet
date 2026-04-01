#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet;
using AioTieba4DotNet.Api.GetFans;
using AioTieba4DotNet.Api.GetFid;
using AioTieba4DotNet.Api.GetForum;
using AioTieba4DotNet.Api.GetSelfInfoInitNickname;
using AioTieba4DotNet.Api.GetSelfInfoMoIndex;
using AioTieba4DotNet.Api.GetUInfoGetUserInfoWeb;
using AioTieba4DotNet.Api.GetUInfoPanel;
using AioTieba4DotNet.Api.GetUInfoUserJson;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api;

[TestClass]
public sealed class UserProfileWrapperBranchCoverageTests
{
    [TestMethod]
    public async Task UserProfileWrappers_CoverHappyBranchesAndRequestShapes()
    {
        var httpCore = new RoutingHttpCore
        {
            Account = new Account(new string('b', 192), new string('s', 64))
        };
        httpCore.EnqueueAppFormResponse("/c/u/fans/page", """
                                                     {"error_code":0,"error_msg":"","user_list":[{"id":42,"portrait":"tb.1.target","name":"target-user","name_show":"Target"}],"page":{"current_page":2,"has_more":1,"has_prev":0}}
                                                     """);
        httpCore.EnqueueWebGetResponse("/f/commit/share/fnameShareApi", """
                                                 {"no":0,"error":"","data":{"fid":7356044}}
                                                 """);
        httpCore.EnqueueAppFormResponse("/c/f/frs/frsBottom", """
                                                     {"error_code":0,"error_msg":"","forum":{"id":7356044,"name":"lol欧服吧","first_class":"游戏","second_class":"英雄联盟","avatar":"avatar","slogan":"safe forum","member_num":1,"post_num":2,"thread_num":3,"managers":[]}}
                                                     """);
        httpCore.EnqueueAppFormResponse("/c/s/initNickname", """
                                                     {"error_code":0,"error_msg":"","user_info":{"user_name":"self-user","name_show":"Old Name","tieba_uid":778899}}
                                                     """);
        httpCore.EnqueueWebGetResponse("/mo/q/newmoindex", """
                                                 {"no":0,"error":"","data":{"id":123,"portrait":"tb.1.safe","name":"mo-user","user_sex":1,"post_num":12,"fans_num":34,"concern_num":56,"like_forum_num":78,"intro":"hello","vipInfo":{"v_status":3}}}
                                                 """);
        httpCore.EnqueueWebGetResponse("/im/pcmsg/query/getUserInfo", """
                                                 {"errno":0,"errmsg":"","chatUser":{"uid":123,"uname":"123","portrait":"tb.1.safe?from=pc","show_nickname":"Safe User"}}
                                                 """);
        httpCore.EnqueueAppFormResponse("/home/get/panel", """
                                                     {"no":0,"error":"","data":{"portrait":"tb.1.safe?from=pc","name":"safe-user","show_nickname":"Safe User","name_show":"Safe User","gender":"male","tb_age":"-","post_num":"12","followed_count":"34","vipInfo":{"v_status":3}}}
                                                     """);
        httpCore.EnqueueAppFormResponse("/home/get/panel", """
                                                     {"no":0,"error":"","data":{"portrait":"safe-user","name":"safe-user","show_nickname":"Safe User","name_show":"Safe User","gender":"male","tb_age":"2.5","post_num":"12","followed_count":"34","vipInfo":{"v_status":0}}}
                                                     """);
        httpCore.EnqueueWebGetResponse("/i/sys/user_json", """
                                                 {"creator":{"id":321,"portrait":"tb.1.safe?from=pc","name":"safe-user","name_show":"Safe User"}}
                                                 """);

        var fans = await new GetFans(httpCore).RequestAsync(42, 2);
        Assert.AreEqual("42", httpCore.GetAppFormValue("uid"));
        Assert.AreEqual("2", httpCore.GetAppFormValue("pn"));
        var fid = await new GetFid(httpCore).RequestAsync("lol欧服吧");
        Assert.AreEqual("/f/commit/share/fnameShareApi", httpCore.LastWebGetUri?.AbsolutePath);
        Assert.AreEqual("lol欧服吧", httpCore.GetWebGetValue("fname"));
        var forum = await new GetForum(httpCore).RequestAsync("lol欧服吧");
        Assert.AreEqual("/c/f/frs/frsBottom", httpCore.LastAppFormUri?.AbsolutePath);
        var initNickname = await new GetSelfInfoInitNickname(httpCore).RequestAsync();
        var moIndex = await new GetSelfInfoMoIndex(httpCore).RequestAsync();
        var basicWeb = await new GetUInfoGetUserInfoWeb(httpCore).RequestAsync(123);
        var panelByPortrait = await new GetUInfoPanel(httpCore).RequestAsync("tb.1.safe");
        Assert.AreEqual("id", httpCore.LastAppFormData[0].Key);
        Assert.AreEqual("tb.1.safe", httpCore.LastAppFormValue("id"));
        var panelByName = await new GetUInfoPanel(httpCore).RequestAsync("safe-user");
        var userJson = await new GetUInfoUserJson(httpCore).RequestAsync("safe-user");

        Assert.AreEqual(1, fans.Count);
        Assert.AreEqual(42L, fans[0].UserId);
        Assert.AreEqual(7356044UL, fid);
        Assert.AreEqual((long)7356044, forum.Fid);
        Assert.AreEqual("lol欧服吧", forum.Fname);
        Assert.AreEqual("self-user", initNickname.UserName);
        Assert.AreEqual("Old Name", initNickname.NickNameOld);
        Assert.AreEqual(778899, initNickname.TiebaUid);
        Assert.AreEqual(123, moIndex.UserId);
        Assert.AreEqual("mo-user", moIndex.UserName);
        Assert.IsTrue(moIndex.IsVip);
        Assert.AreEqual(string.Empty, basicWeb.UserName);
        Assert.AreEqual("tb.1.safe", basicWeb.Portrait);
        Assert.AreEqual("Safe User", basicWeb.NickNameNew);
        Assert.AreEqual("tb.1", panelByPortrait.Portrait);
        Assert.AreEqual("safe-user", panelByName.UserName);
        Assert.AreEqual("un", httpCore.LastAppFormData[0].Key);
        Assert.AreEqual("safe-user", httpCore.LastAppFormValue("un"));
        Assert.AreEqual(321, userJson.UserId);
        Assert.AreEqual("tb.1", userJson.Portrait);
        Assert.AreEqual("safe-user", userJson.UserName);
        Assert.AreEqual("Safe User", userJson.NickNameNew);
    }

    [TestMethod]
    public async Task UserProfileWrappers_CoverFallbackBranches_AndThrowPaths()
    {
        var httpCore = new RoutingHttpCore
        {
            Account = new Account(new string('b', 192), new string('s', 64))
        };
        httpCore.EnqueueAppFormResponse("/c/u/fans/page", """
                                                     {"error_code":0,"error_msg":"","page":{"current_page":0,"has_more":0,"has_prev":0}}
                                                     """);
        httpCore.EnqueueWebGetResponse("/f/commit/share/fnameShareApi", """
                                                 {"no":0,"error":"","data":{"fid":0}}
                                                 """);
        httpCore.EnqueueAppFormResponse("/c/f/frs/frsBottom", """
                                                     {"error_code":0,"error_msg":"","not_forum":{}}
                                                     """);
        httpCore.EnqueueAppFormResponse("/c/s/initNickname", """
                                                     {"error_code":0,"error_msg":""}
                                                     """);
        httpCore.EnqueueWebGetResponse("/mo/q/newmoindex", """
                                                 {"no":0,"error":""}
                                                 """);
        httpCore.EnqueueWebGetResponse("/im/pcmsg/query/getUserInfo", """
                                                 {"errno":0,"errmsg":""}
                                                 """);
        httpCore.EnqueueAppFormResponse("/home/get/panel", """
                                                     {"no":0,"error":""}
                                                     """);
        httpCore.EnqueueWebGetResponse("/i/sys/user_json", string.Empty);
        httpCore.EnqueueWebGetResponse("/i/sys/user_json", """
                                                 {"foo":1}
                                                 """);

        var fans = await new GetFans(httpCore).RequestAsync(42, 2);
        var fidException = await ThrowsAsync<TieBaServerException>(async () =>
            await new GetFid(httpCore).RequestAsync("lol欧服吧"));
        var forumException = await ThrowsAsync<TieBaServerException>(async () =>
            await new GetForum(httpCore).RequestAsync("lol欧服吧"));
        var initNickname = await new GetSelfInfoInitNickname(httpCore).RequestAsync();
        var moIndex = await new GetSelfInfoMoIndex(httpCore).RequestAsync();
        var basicWebException = await ThrowsAsync<TieBaServerException>(async () =>
            await new GetUInfoGetUserInfoWeb(httpCore).RequestAsync(123));
        var panelException = await ThrowsAsync<TieBaServerException>(async () =>
            await new GetUInfoPanel(httpCore).RequestAsync("safe-user"));
        var userJsonEmptyException = await ThrowsAsync<TieBaServerException>(async () =>
            await new GetUInfoUserJson(httpCore).RequestAsync("safe-user"));
        var userJsonCreatorException = await ThrowsAsync<TieBaServerException>(async () =>
            await new GetUInfoUserJson(httpCore).RequestAsync("safe-user"));

        Assert.AreEqual(0, fans.Count);
        Assert.AreEqual(-1, fidException.Code);
        Assert.AreEqual(-1, forumException.Code);
        Assert.AreEqual(string.Empty, initNickname.UserName);
        Assert.AreEqual(string.Empty, initNickname.NickNameOld);
        Assert.AreEqual(0, initNickname.TiebaUid);
        Assert.AreEqual(0, moIndex.UserId);
        Assert.AreEqual(string.Empty, moIndex.Portrait);
        Assert.AreEqual(string.Empty, moIndex.UserName);
        Assert.AreEqual(string.Empty, moIndex.Sign);
        Assert.AreEqual(-1, basicWebException.Code);
        Assert.AreEqual(-1, panelException.Code);
        Assert.AreEqual(-1, userJsonEmptyException.Code);
        Assert.AreEqual(-1, userJsonCreatorException.Code);
    }

    private sealed class RoutingHttpCore : ITiebaHttpCore
    {
        private readonly Dictionary<string, Queue<string>> _appFormResponses = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Queue<string>> _webGetResponses = new(StringComparer.OrdinalIgnoreCase);

        public Account? Account { get; set; }

        public HttpClient HttpClient { get; } = new();

        public Uri? LastAppFormUri { get; private set; }
        public List<KeyValuePair<string, string>> LastAppFormData { get; private set; } = [];
        public Uri? LastWebGetUri { get; private set; }
        public List<KeyValuePair<string, string>> LastWebGetParameters { get; private set; } = [];

        public void EnqueueAppFormResponse(string path, string response)
        {
            Enqueue(_appFormResponses, path, response);
        }

        public void EnqueueWebGetResponse(string path, string response)
        {
            Enqueue(_webGetResponses, path, response);
        }

        public void SetAccount(Account newAccount) => Account = newAccount;

        public Task<string> SendAsync(Func<HttpRequestMessage> requestFactory, bool allowRetry = false,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<string> SendAppFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default)
        {
            LastAppFormUri = uri;
            LastAppFormData = [.. data];
            return Task.FromResult(Dequeue(_appFormResponses, uri.AbsolutePath));
        }

        public Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters,
            CancellationToken cancellationToken = default)
        {
            LastWebGetUri = uri;
            LastWebGetParameters = [.. parameters];
            return Task.FromResult(Dequeue(_webGetResponses, uri.AbsolutePath));
        }

        public Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public string GetAppFormValue(string key) => LastAppFormData.Last(entry => entry.Key == key).Value;

        public string LastAppFormValue(string key) => GetAppFormValue(key);

        public string GetWebGetValue(string key) => LastWebGetParameters.Last(entry => entry.Key == key).Value;

        private static void Enqueue(Dictionary<string, Queue<string>> responses, string path, string response)
        {
            if (!responses.TryGetValue(path, out var queue))
            {
                queue = new Queue<string>();
                responses[path] = queue;
            }

            queue.Enqueue(response);
        }

        private static string Dequeue(Dictionary<string, Queue<string>> responses, string path)
        {
            if (!responses.TryGetValue(path, out var queue) || queue.Count == 0)
                throw new InvalidOperationException($"No queued response for '{path}'.");

            return queue.Dequeue();
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
