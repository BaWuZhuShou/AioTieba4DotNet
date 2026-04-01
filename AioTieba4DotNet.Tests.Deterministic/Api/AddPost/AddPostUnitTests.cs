using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Transport;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.AddPost;

[TestClass]
public class AddPostUnitTests
{
    [TestMethod]
    public async Task RequestWsAsync_UsesWebSocketExecutor()
    {
        // Arrange
        var account = new Account(new string('a', 192), new string('b', 64));
        var fakeHttp = new FakeHttpCore { Account = account };
        var fakeWs = new FakeWsCore { Account = account };

        // 构造成功的 Protobuf 响应
        var resIdl = new AddPostResIdl
        {
            Error = new Error { Errorno = 0 }, Data = new AddPostResIdl.Types.DataRes { Pid = 998877 }
        };
        fakeWs.Response = resIdl.ToByteArray();

        var api = new AioTieba4DotNet.Api.AddPost.AddPost(fakeHttp, fakeWs);
        var content = "hello";

        // Act
        var success = await api.RequestWsAsync("test_forum", 1, 2, content);

        // Assert
        Assert.IsTrue(success);
        Assert.AreEqual(309731, fakeWs.LastCmd);
        Assert.IsNotNull(fakeWs.LastData);

        var reqIdl = AddPostReqIdl.Parser.ParseFrom(fakeWs.LastData);
        Assert.AreEqual("test_forum", reqIdl.Data.Kw);
        Assert.AreEqual("1", reqIdl.Data.Fid);
        Assert.AreEqual("2", reqIdl.Data.Tid);
        Assert.Contains("hello", reqIdl.Data.Content);
    }

    [TestMethod]
    public async Task RequestWsAsync_ThrowsOnNeedVcode_Proto()
    {
        // Arrange
        var account = new Account(new string('a', 192), new string('b', 64));
        var fakeHttp = new FakeHttpCore { Account = account };
        var fakeWs = new FakeWsCore { Account = account };

        // 构造需要验证码的 Protobuf 响应
        var resIdl = new AddPostResIdl
        {
            Error = new Error { Errorno = 0 },
            Data = new AddPostResIdl.Types.DataRes
            {
                Info = new AddPostResIdl.Types.DataRes.Types.PostAntiInfo { NeedVcode = "1" }
            }
        };
        fakeWs.Response = resIdl.ToByteArray();

        var api = new AioTieba4DotNet.Api.AddPost.AddPost(fakeHttp, fakeWs);
        var content = "hello";

        // Act & Assert
        try
        {
            await api.RequestWsAsync("test_forum", 1, 2, content);
            Assert.Fail("Expected TiebaException was not thrown.");
        }
        catch (TiebaException)
        {
            // Expected
        }
    }

    [TestMethod]
    public async Task RequestHttpAsync_UsesEmptyOptionalAccountFieldsInPackedProto()
    {
        var account = new Account(new string('a', 192), new string('b', 64));
        account.ClientId = null;
        account.Tbs = null;
        account.SampleId = null;
        account.C3Aid = null;
        account.ZId = null;

        var fakeHttp = new FakeHttpCore
        {
            Account = account, ProtoResponse = new AddPostResIdl { Error = new Error { Errorno = 0 } }.ToByteArray()
        };
        var fakeWs = new FakeWsCore { Account = account };
        var api = new AioTieba4DotNet.Api.AddPost.AddPost(fakeHttp, fakeWs);

        var success = await api.RequestHttpAsync("test_forum", 1, 2, "hello", null);

        Assert.IsTrue(success);
        var reqIdl = AddPostReqIdl.Parser.ParseFrom(fakeHttp.LastProtoData);
        Assert.AreEqual(string.Empty, reqIdl.Data.Common.ClientId);
        Assert.AreEqual(string.Empty, reqIdl.Data.Common.Tbs);
        Assert.AreEqual(string.Empty, reqIdl.Data.Common.SampleId);
        Assert.AreEqual(string.Empty, reqIdl.Data.Common.ZId);
    }

    [TestMethod]
    public async Task RequestHttpAsync_PacksExplicitOptionalAccountFields_AndAllowsMissingResponseData()
    {
        var account = new Account(new string('a', 192), new string('b', 64))
        {
            ClientId = "client-123",
            Tbs = "tbs-456",
            SampleId = "sample-789",
            C3Aid = "c3aid-101",
            ZId = "zid-202"
        };
        var fakeHttp = new FakeHttpCore
        {
            Account = account, ProtoResponse = new AddPostResIdl { Error = new Error { Errorno = 0 } }.ToByteArray()
        };
        var fakeWs = new FakeWsCore { Account = account };
        var api = new AioTieba4DotNet.Api.AddPost.AddPost(fakeHttp, fakeWs);

        var success = await api.RequestHttpAsync("test_forum", 1, 2, "hello", "Shown Name");

        var reqIdl = AddPostReqIdl.Parser.ParseFrom(fakeHttp.LastProtoData);

        Assert.IsTrue(success);
        Assert.AreEqual("Shown Name", reqIdl.Data.NameShow);
        Assert.AreEqual("client-123", reqIdl.Data.Common.ClientId);
        Assert.AreEqual("tbs-456", reqIdl.Data.Common.Tbs);
        Assert.AreEqual("sample-789", reqIdl.Data.Common.SampleId);
        Assert.AreEqual("c3aid-101", reqIdl.Data.Common.C3Aid);
        Assert.AreEqual("zid-202", reqIdl.Data.Common.ZId);
    }

    [TestMethod]
    public async Task RequestHttpAndWsAsync_PackExplicitShowName_OnBothTransportPaths()
    {
        var account = new Account(new string('a', 192), new string('b', 64));

        var fakeHttp = new FakeHttpCore
        {
            Account = account, ProtoResponse = new AddPostResIdl { Error = new Error { Errorno = 0 } }.ToByteArray()
        };
        var fakeWs = new FakeWsCore
        {
            Account = account, Response = new AddPostResIdl { Error = new Error { Errorno = 0 } }.ToByteArray()
        };
        var api = new AioTieba4DotNet.Api.AddPost.AddPost(fakeHttp, fakeWs);

        var httpSuccess = await api.RequestHttpAsync("test_forum", 1, 2, "hello", "Shown Name");
        var wsSuccess = await api.RequestWsAsync("test_forum", 1, 2, "hello", "Shown Name");

        var httpReqIdl = AddPostReqIdl.Parser.ParseFrom(fakeHttp.LastProtoData);
        var wsReqIdl = AddPostReqIdl.Parser.ParseFrom(fakeWs.LastData);

        Assert.IsTrue(httpSuccess);
        Assert.IsTrue(wsSuccess);
        Assert.AreEqual("Shown Name", httpReqIdl.Data.NameShow);
        Assert.AreEqual("Shown Name", wsReqIdl.Data.NameShow);
    }

    [TestMethod]
    public async Task RequestHttpAndWsAsync_ConcurrentRequests_CoverLazyAccountCacheBranches()
    {
        var account = new Account(new string('a', 192), new string('b', 64));
        var response = new AddPostResIdl { Error = new Error { Errorno = 0 } }.ToByteArray();

        var fakeHttp = new FakeHttpCore { Account = account, ProtoResponse = response };
        var fakeWs = new FakeWsCore { Account = account, Response = response };
        var api = new AioTieba4DotNet.Api.AddPost.AddPost(fakeHttp, fakeWs);

        using var startGate = new ManualResetEventSlim();

        Task<bool> RunHttpAsync()
        {
            return Task.Run(async () =>
            {
                startGate.Wait();
                return await api.RequestHttpAsync("test_forum", 1, 2, "hello", "Shown Name");
            });
        }

        Task<bool> RunWsAsync()
        {
            return Task.Run(async () =>
            {
                startGate.Wait();
                return await api.RequestWsAsync("test_forum", 1, 2, "hello", "Shown Name");
            });
        }

        var tasks = new[] { RunHttpAsync(), RunWsAsync(), RunHttpAsync(), RunWsAsync() };
        startGate.Set();
        var results = await Task.WhenAll(tasks);

        Assert.IsTrue(results[0]);
        Assert.IsTrue(results[1]);
        Assert.IsTrue(results[2]);
        Assert.IsTrue(results[3]);
    }

    private class FakeHttpCore : ITiebaHttpCore
    {
        public string Response { get; } = string.Empty;
        public byte[] ProtoResponse { get; set; } = [];
        public List<KeyValuePair<string, string>> LastData { get; private set; }
        public byte[] LastProtoData { get; private set; }
        public Account Account { get; set; }
        public HttpClient HttpClient => throw new NotImplementedException();

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
            LastData = data;
            return Task.FromResult(Response);
        }

        public Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data, CancellationToken cancellationToken = default)
        {
            LastProtoData = data;
            return Task.FromResult(ProtoResponse);
        }

        public Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private class FakeWsCore : ITiebaWsCore
    {
        public bool IsConnected => true;
        public byte[] Response { get; set; } = [];
        public int LastCmd { get; private set; }
        public byte[] LastData { get; private set; }
        public Account Account { get; set; }

        public Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task CloseAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SendAsync(WSReq req, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<WSRes> SendAsync(int cmd, byte[] data, bool encrypt = true,
            CancellationToken cancellationToken = default)
        {
            LastCmd = cmd;
            LastData = data;

            var wsRes = new WSRes { Payload = new WSRes.Types.Payload { Data = ByteString.CopyFrom(Response) } };
            return Task.FromResult(wsRes);
        }

        public IAsyncEnumerable<WSRes> ListenAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void SetAccount(Account account)
        {
            Account = account;
        }
    }
}
