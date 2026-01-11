using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.AddThread;
using AioTieba4DotNet.Api.Entities.Contents;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;

namespace AioTieba4DotNet.Tests.Api.AddThread;

[TestClass]
public class AddThreadUnitTests
{
    private class FakeHttpCore : ITiebaHttpCore
    {
        public Account? Account { get; set; }
        public HttpClient HttpClient => throw new NotImplementedException();
        public string Response { get; set; } = string.Empty;
        public List<KeyValuePair<string, string>>? LastData { get; private set; }

        public void SetAccount(Account newAccount) => Account = newAccount;

        public Task<string> SendAppFormAsync(Uri uri, List<KeyValuePair<string, string>> data)
        {
            LastData = data;
            return Task.FromResult(Response);
        }

        public Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data) => throw new NotImplementedException();
        public Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters) => throw new NotImplementedException();
        public Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data) => throw new NotImplementedException();
    }

    private class FakeWsCore : ITiebaWsCore
    {
        public Account? Account { get; set; }
        public bool IsConnected => true;
        public Task ConnectAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CloseAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendAsync(WSReq req, System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<WSRes> SendAsync(int cmd, byte[] data, bool encrypt = true, System.Threading.CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<WSRes> ListenAsync(System.Threading.CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public void SetAccount(Account account) => Account = account;
    }

    [TestMethod]
    public async Task TestRequestAsync_SendsAllParameters()
    {
        // Arrange
        var fakeBduss = new string('a', 192);
        var fakeStoken = new string('b', 64);
        var account = new Account(fakeBduss, fakeStoken)
        {
            ClientId = "fake_client_id",
            ZId = "fake_z_id"
        };
        var fakeHttp = new FakeHttpCore { Account = account, Response = "{\"error_code\": 0, \"data\": {\"tid\": 987654}}" };
        var fakeWs = new FakeWsCore();
        var api = new AioTieba4DotNet.Api.AddThread.AddThread(fakeHttp, fakeWs);
        var contents = new List<IFrag> { new FragText { Text = "hello world" } };

        // Act
        var tid = await api.RequestAsync("test_forum", 123, "test title", contents);

        // Assert
        Assert.AreEqual(987654L, tid);
        var data = fakeHttp.LastData;
        Assert.IsNotNull(data);

        var dict = data.ToDictionary(k => k.Key, v => v.Value);
        Assert.AreEqual(fakeBduss, dict["BDUSS"]);
        Assert.AreEqual(fakeStoken, dict["stoken"]);
        Assert.AreEqual("fake_client_id", dict["_client_id"]);
        Assert.AreEqual("fake_z_id", dict["z_id"]);
        Assert.AreEqual("test_forum", dict["fname"]);
        Assert.AreEqual("123", dict["fid"]);
        Assert.AreEqual("test title", dict["title"]);
        Assert.IsTrue(dict.ContainsKey("content"));
        Assert.AreEqual("3", dict["post_from"]);
    }

    [TestMethod]
    public async Task TestRequestAsync_ThrowsOnNeedVcode()
    {
        // Arrange
        var fakeBduss = new string('a', 192);
        var fakeStoken = new string('b', 64);
        var account = new Account(fakeBduss, fakeStoken);
        var fakeHttp = new FakeHttpCore
        {
            Account = account,
            Response = "{\"error_code\": 0, \"data\": {\"info\": {\"need_vcode\": 1}}}"
        };
        var fakeWs = new FakeWsCore();
        var api = new AioTieba4DotNet.Api.AddThread.AddThread(fakeHttp, fakeWs);
        var contents = new List<IFrag> { new FragText { Text = "hello" } };

        // Act & Assert
        bool exceptionThrown = false;
        try
        {
            await api.RequestAsync("test_forum", 1, "title", contents);
        }
        catch (TiebaException ex) when (ex.Message == "Need verify code")
        {
            exceptionThrown = true;
        }
        Assert.IsTrue(exceptionThrown, "Should throw TiebaException with message 'Need verify code'");
    }
}
