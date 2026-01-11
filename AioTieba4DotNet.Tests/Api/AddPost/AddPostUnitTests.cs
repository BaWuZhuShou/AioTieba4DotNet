using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Enums;
using AioTieba4DotNet.Exceptions;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.AddPost;

[TestClass]
public class AddPostUnitTests
{
    [TestMethod]
    public async Task TestRequestAsync_WsMode()
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

        var api = new AioTieba4DotNet.Api.AddPost.AddPost(fakeHttp, fakeWs, TiebaRequestMode.Websocket);
        var content = "hello";

        // Act
        var success = await api.RequestAsync("test_forum", 1, 2, content);

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
    public async Task TestRequestAsync_ThrowsOnNeedVcode_Proto()
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

        var api = new AioTieba4DotNet.Api.AddPost.AddPost(fakeHttp, fakeWs, TiebaRequestMode.Websocket);
        var content = "hello";

        // Act & Assert
        try
        {
            await api.RequestAsync("test_forum", 1, 2, content);
            Assert.Fail("Expected TiebaException was not thrown.");
        }
        catch (TiebaException)
        {
            // Expected
        }
    }

    private class FakeHttpCore : ITiebaHttpCore
    {
        public string Response { get; } = string.Empty;
        public byte[] ProtoResponse { get; } = [];
        public List<KeyValuePair<string, string>> LastData { get; private set; }
        public byte[] LastProtoData { get; private set; }
        public Account Account { get; set; }
        public HttpClient HttpClient => throw new NotImplementedException();

        public void SetAccount(Account newAccount)
        {
            Account = newAccount;
        }

        public Task<string> SendAppFormAsync(Uri uri, List<KeyValuePair<string, string>> data)
        {
            LastData = data;
            return Task.FromResult(Response);
        }

        public Task<string> GetTbsAsync()
        {
            return Task.FromResult(Account?.Tbs ?? string.Empty);
        }

        public Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data)
        {
            LastProtoData = data;
            return Task.FromResult(ProtoResponse);
        }

        public Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters)
        {
            throw new NotImplementedException();
        }

        public Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data)
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
