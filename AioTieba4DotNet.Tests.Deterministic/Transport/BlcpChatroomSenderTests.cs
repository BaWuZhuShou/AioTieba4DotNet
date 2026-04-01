#nullable enable
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport.Chatrooms;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Tests.Transport;

[TestClass]
public sealed class BlcpChatroomSenderTests
{
    [TestMethod]
    public async Task SendMessageAsync_RejectsNullArgumentsAndMissingSampleIdBeforeNetwork()
    {
        var sender = new BlcpChatroomSender();
        var account = new Account(new string('b', 192), new string('s', 64));
        var self = new UserInfo { UserId = 42, UserName = "sender", Portrait = "tb.1.sender" };
        var level = new ForumLevelInfo { UserLevel = 9 };

        await ThrowsAsync<ArgumentNullException>(() =>
            sender.SendMessageAsync(null!, self, level, 1, 2, "hi", null, -1, CancellationToken.None));
        await ThrowsAsync<ArgumentNullException>(() =>
            sender.SendMessageAsync(account, null!, level, 1, 2, "hi", null, -1, CancellationToken.None));
        await ThrowsAsync<ArgumentNullException>(() =>
            sender.SendMessageAsync(account, self, null!, 1, 2, "hi", null, -1, CancellationToken.None));

        var exception = await ThrowsAsync<TiebaConfigurationException>(() =>
            sender.SendMessageAsync(account, self, level, 1, 2, "hi", null, -1, CancellationToken.None));

        StringAssert.Contains(exception.Message, nameof(Account.SampleId));
    }

    [TestMethod]
    public void BlcpHelpers_BuildRpcBodyAndAtData_ReturnExpectedShapes()
    {
        var rpcBody = InvokeStatic<byte[]>(typeof(BlcpChatroomSender), "BuildRpcBody", 3L, 185L, 123456L, 1);
        var rpcMeta = RpcMeta.Parser.ParseFrom(rpcBody);
        var atData = InvokeStatic<JArray>(typeof(BlcpChatroomSender), "BuildAtData", (IReadOnlyList<long>)[11L, 22L]);

        Assert.AreEqual(3L, rpcMeta.Request.ServiceId);
        Assert.AreEqual(185L, rpcMeta.Request.MethodId);
        Assert.AreEqual(123456L, rpcMeta.Request.LogId);
        Assert.AreEqual(1, rpcMeta.Request.NeedCommon);
        Assert.AreEqual("CLCPReqBegin", rpcMeta.Request.EventList[0].Event);
        Assert.AreEqual("11", atData[0]!["uid"]!.Value<string>());
        Assert.AreEqual("22", atData[1]!["uid"]!.Value<string>());
    }

    [TestMethod]
    public void BlcpHelpers_BuildMainData_HandlesVipAndNonVipBranches()
    {
        var vipMainData = InvokeStatic<JArray>(typeof(BlcpChatroomSender), "BuildMainData", 9, true, 12, 7356044UL,
            "tb.1.sender", "Sender");
        var normalMainData = InvokeStatic<JArray>(typeof(BlcpChatroomSender), "BuildMainData", 3, false, 5, 7356044UL,
            "tb.1.sender", "Sender");

        Assert.AreEqual(4, vipMainData.Count);
        Assert.AreEqual(3, normalMainData.Count);
        Assert.AreEqual("Sender", vipMainData[1]!["text"]!["str"]!.Value<string>());
        Assert.IsNotNull(vipMainData[1]!["text"]!["text_color"]);
        Assert.IsNull(normalMainData[0]!["text"]?["text_color"]);
        StringAssert.Contains(vipMainData[^1]!["icon"]!["schema"]!.Value<string>(), "forum_id=7356044");
        StringAssert.Contains(vipMainData[^2]!["icon"]!["url"]!.Value<string>(), "usergrouth_12");
        StringAssert.Contains(vipMainData[^1]!["icon"]!["url"]!.Value<string>(), "icon_level_09");
    }

    [TestMethod]
    public void BlcpHelpers_ProduceStableFormatsForBdukMd5AndMessageKey()
    {
        var bduk = InvokeStatic<string>(typeof(BlcpChatroomSender), "GetBdukFromUserId", "42");
        var msgKey = InvokeStatic<string>(typeof(BlcpChatroomSender), "GetMsgKey", bduk);
        var md5 = InvokeStatic<string>(typeof(BlcpChatroomSender), "ComputeMd5Hex", "abc");
        var correlationId = InvokeStatic<long>(typeof(BlcpChatroomSender), "CreateCorrelationId");

        Assert.IsFalse(string.IsNullOrWhiteSpace(bduk));
        Assert.IsFalse(bduk.Contains('+'));
        Assert.IsFalse(bduk.Contains('/'));
        Assert.IsTrue(msgKey.StartsWith(bduk, StringComparison.Ordinal));
        Assert.AreEqual("900150983cd24fb0d6963f7d28e17f72", md5);
        Assert.IsGreaterThan(0L, correlationId);
    }

    [TestMethod]
    public void BlcpHelpers_DecompressGzip_And_ReadExactAsync_WorkAsExpected()
    {
        byte[] compressed;
        using (var target = new MemoryStream())
        {
            using var gzip = new GZipStream(target, CompressionLevel.SmallestSize, true);
            gzip.Write(Encoding.UTF8.GetBytes("hello world"));
            gzip.Flush();
            gzip.Dispose();
            compressed = target.ToArray();
        }

        var decompressed = InvokeStatic<byte[]>(typeof(BlcpChatroomSender), "DecompressGzip", compressed);
        Assert.AreEqual("hello world", Encoding.UTF8.GetString(decompressed));

        var buffer = new byte[5];
        var stream = new MemoryStream([1, 2, 3, 4, 5]);
        InvokeStaticTask(typeof(BlcpChatroomSender), "ReadExactAsync", stream, buffer, CancellationToken.None)
            .GetAwaiter().GetResult();
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5 }, buffer);

        var eof = Throws<EndOfStreamException>(() =>
            InvokeStaticTask(typeof(BlcpChatroomSender), "ReadExactAsync", new MemoryStream([1, 2]), new byte[3],
                    CancellationToken.None)
                .GetAwaiter().GetResult());
        StringAssert.Contains(eof.Message, "closed unexpectedly");
    }

    [TestMethod]
    public void EnuidCodec_EncodesValuesAndRejectsBlankInput()
    {
        var encoded = EnuidCodec.Encode("123456");
        var encodedLonger = EnuidCodec.Encode("galaxy-cuid");
        var blank = Throws<ArgumentException>(() => EnuidCodec.Encode(" "));

        Assert.IsFalse(string.IsNullOrWhiteSpace(encoded));
        Assert.IsFalse(string.IsNullOrWhiteSpace(encodedLonger));
        StringAssert.Contains(blank.Message, "cuidGalaxy2");
    }

    [TestMethod]
    public void EnuidCodec_PrivateHelpers_CoverLookupAndEncodingBranches()
    {
        var buildTable = typeof(EnuidCodec).GetMethod("BuildTable", BindingFlags.NonPublic | BindingFlags.Static)
                         ?? throw new InvalidOperationException("BuildTable not found.");
        var xorWords = typeof(EnuidCodec).GetMethod("XorWords", BindingFlags.NonPublic | BindingFlags.Static)
                       ?? throw new InvalidOperationException("XorWords not found.");
        var encodeInPlace = typeof(EnuidCodec).GetMethod("EncodeInPlace", BindingFlags.NonPublic | BindingFlags.Static)
                            ?? throw new InvalidOperationException("EncodeInPlace not found.");

        var table = ((int[] State, byte[] Map))buildTable.Invoke(null, [7, true])!;
        var remainderBuffer = new byte[12];
        Encoding.UTF8.GetBytes("cuid7", remainderBuffer);
        var alignedBuffer = new byte[12];
        Encoding.UTF8.GetBytes("abc", alignedBuffer);
        var emptyEncodedLength = (int)encodeInPlace.Invoke(null, [Array.Empty<byte>(), 0, 0])!;

        var remainderEncodedLength = (int)encodeInPlace.Invoke(null, [remainderBuffer, 5, 7])!;
        var alignedEncodedLength = (int)encodeInPlace.Invoke(null, [alignedBuffer, 3, 0])!;

        Assert.IsTrue(table.Map.Skip(4).Take(128).Any(static value => value != 0));
        Assert.IsTrue(table.Map.Skip(4).Take(128).All(static value => value <= 64));
        Assert.AreEqual(-1, emptyEncodedLength);
        Assert.AreEqual(10, remainderEncodedLength);
        Assert.AreEqual(6, alignedEncodedLength);
        Assert.AreEqual('C', (char)remainderBuffer[8]);
        Assert.AreEqual('A', (char)alignedBuffer[4]);
        CollectionAssert.AreNotEqual(new byte[] { (byte)'c', (byte)'u', (byte)'i', (byte)'d', (byte)'7' },
            remainderBuffer[..5]);

        var stateOnly = ((int[] State, byte[] Map))buildTable.Invoke(null, [0, false])!;
        var wholeWordBuffer = new byte[] { 1, 2, 3, 4 };
        xorWords.Invoke(null, [stateOnly.State, wholeWordBuffer, 4]);

        CollectionAssert.AreNotEqual(new byte[] { 1, 2, 3, 4 }, wholeWordBuffer);
    }

    [TestMethod]
    public async Task BlcpFrames_ReadFrame_RejectsInvalidPrefixAndLengths_AndDecompressesPayload()
    {
        await WithAuthenticatedTlsStreamsAsync(async (clientStream, serverStream) =>
        {
            var writerTask =
                WriteBytesAsync(serverStream, [(byte)'x', (byte)'c', (byte)'p', 1], CancellationToken.None);

            var exception = await ThrowsAsync<TiebaProtocolException>(() =>
                InvokeStaticTaskWithResultAsync(typeof(BlcpChatroomSender), "ReadFrameAsync", clientStream,
                    CancellationToken.None));

            StringAssert.Contains(exception.Message, "prefix");
            await writerTask;
            return 0;
        });

        await WithAuthenticatedTlsStreamsAsync(async (clientStream, serverStream) =>
        {
            var header = new byte[12];
            header[0] = (byte)'l';
            header[1] = (byte)'c';
            header[2] = (byte)'p';
            header[3] = 1;
            BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4, 4), 1);
            BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(8, 4), 2);
            var writerTask = WriteBytesAsync(serverStream, header, CancellationToken.None);

            var exception = await ThrowsAsync<TiebaProtocolException>(() =>
                InvokeStaticTaskWithResultAsync(typeof(BlcpChatroomSender), "ReadFrameAsync", clientStream,
                    CancellationToken.None));

            StringAssert.Contains(exception.Message, "lengths");
            await writerTask;
            return 0;
        });

        await WithAuthenticatedTlsStreamsAsync(async (clientStream, serverStream) =>
        {
            var payload = Encoding.UTF8.GetBytes("{\"message\":\"hello\"}");
            var writerTask = WriteBlcpFrameAsync(serverStream,
                CreateRpcResponseBody(1),
                CompressGzip(payload),
                CancellationToken.None);

            var result = await InvokeStaticTaskWithResultAsync(typeof(BlcpChatroomSender), "ReadFrameAsync",
                clientStream, CancellationToken.None);
            var rpcMeta = (RpcMeta)GetTupleItem(result!, "Item1");
            var lcmBody = (byte[])GetTupleItem(result!, "Item2");

            Assert.AreEqual(1, rpcMeta.CompressType);
            Assert.AreEqual("{\"message\":\"hello\"}", Encoding.UTF8.GetString(lcmBody));
            await writerTask;
            return 0;
        });
    }

    [TestMethod]
    public async Task BlcpHandshake_SendsExpectedPayload_AndRejectsFailureResponses()
    {
        var sender = new BlcpChatroomSender();
        var account = CreateAccount();

        await WithAuthenticatedTlsStreamsAsync(async (clientStream, serverStream) =>
        {
            var serverTask = Task.Run(async () =>
            {
                var request = await ReadBlcpFrameAsync(serverStream, CancellationToken.None);
                var rpcData = RpcData.Parser.ParseFrom(request.LcmBody);

                Assert.AreEqual(1L, request.RpcMeta.Request.ServiceId);
                Assert.AreEqual(1L, request.RpcMeta.Request.MethodId);
                Assert.AreEqual(account.CuidGalaxy2, rpcData.LcmRequest.Common.Cuid);
                Assert.AreEqual("token-1", rpcData.LcmRequest.Token);

                await WriteBlcpFrameAsync(serverStream,
                    CreateRpcResponseBody(),
                    new RpcData { LcmResponse = new LcmResponse { ErrorCode = 0, ErrorMsg = "success" } }.ToByteArray(),
                    CancellationToken.None);
            });

            await InvokeInstanceTaskAsync(sender, "PerformHandshakeAsync", clientStream, account, "token-1",
                CancellationToken.None);
            await serverTask;
            return 0;
        });

        await WithAuthenticatedTlsStreamsAsync(async (clientStream, serverStream) =>
        {
            var serverTask = Task.Run(async () =>
            {
                _ = await ReadBlcpFrameAsync(serverStream, CancellationToken.None);
                await WriteBlcpFrameAsync(serverStream,
                    CreateRpcResponseBody(),
                    new RpcData { LcmResponse = new LcmResponse { ErrorCode = 7, ErrorMsg = "denied" } }.ToByteArray(),
                    CancellationToken.None);
            });

            var exception = await ThrowsAsync<TiebaProtocolException>(() =>
                InvokeInstanceTaskAsync(sender, "PerformHandshakeAsync", clientStream, account, "token-2",
                    CancellationToken.None));

            StringAssert.Contains(exception.Message, "handshake failed");
            await serverTask;
            return 0;
        });
    }

    [TestMethod]
    public async Task BlcpLogin_ParsesExplicitFields_AndFallsBackToSelfInfo()
    {
        var sender = new BlcpChatroomSender();
        var account = CreateAccount();
        var selfInfo = new UserInfo
        {
            UserId = 42,
            UserName = "sender",
            NickNameNew = "Sender",
            Portrait = "tb.1.sender",
            Uk = 88,
            BdUk = "self-bduk"
        };

        await WithAuthenticatedTlsStreamsAsync(async (clientStream, serverStream) =>
        {
            var serverTask = Task.Run(async () =>
            {
                var firstRequest = await ReadBlcpFrameAsync(serverStream, CancellationToken.None);
                var firstJson = JObject.Parse(Encoding.UTF8.GetString(firstRequest.LcmBody));
                Assert.AreEqual(account.SampleId, firstJson["params"]?["sid"]?.Value<string>());
                Assert.AreEqual("tieba", firstJson["params"]?["appname"]?.Value<string>());

                await WriteBlcpFrameAsync(serverStream,
                    CreateRpcResponseBody(),
                    Encoding.UTF8.GetBytes("{\"errno\":0}"),
                    CancellationToken.None);

                var secondRequest = await ReadBlcpFrameAsync(serverStream, CancellationToken.None);
                var secondJson = JObject.Parse(Encoding.UTF8.GetString(secondRequest.LcmBody));
                Assert.AreEqual(account.Bduss, secondJson["token"]?.Value<string>());
                Assert.AreEqual($"android_{account.CuidGalaxy2}", secondJson["device_id"]?.Value<string>());

                await WriteBlcpFrameAsync(serverStream,
                    CreateRpcResponseBody(),
                    Encoding.UTF8.GetBytes("{\"err_code\":0,\"trigger_id\":[99],\"uk\":123,\"bd_uid\":\"login-bduk\"}"),
                    CancellationToken.None);
            });

            var result = await InvokeInstanceTaskWithResultAsync(sender, "PerformLoginAsync", clientStream, account,
                selfInfo, CancellationToken.None);

            Assert.AreEqual(99L, GetPropertyValue<long>(result!, "TriggerId"));
            Assert.AreEqual(123L, GetPropertyValue<long>(result!, "Uk"));
            Assert.AreEqual("login-bduk", GetPropertyValue<string>(result!, "BdUk"));
            await serverTask;
            return 0;
        });

        await WithAuthenticatedTlsStreamsAsync(async (clientStream, serverStream) =>
        {
            var serverTask = Task.Run(async () =>
            {
                _ = await ReadBlcpFrameAsync(serverStream, CancellationToken.None);
                await WriteBlcpFrameAsync(serverStream,
                    CreateRpcResponseBody(),
                    Encoding.UTF8.GetBytes("{\"errno\":0}"),
                    CancellationToken.None);

                _ = await ReadBlcpFrameAsync(serverStream, CancellationToken.None);
                await WriteBlcpFrameAsync(serverStream,
                    CreateRpcResponseBody(),
                    Encoding.UTF8.GetBytes("{\"err_code\":0}"),
                    CancellationToken.None);
            });

            var result = await InvokeInstanceTaskWithResultAsync(sender, "PerformLoginAsync", clientStream, account,
                selfInfo, CancellationToken.None);

            Assert.AreEqual(0L, GetPropertyValue<long>(result!, "TriggerId"));
            Assert.AreEqual(selfInfo.Uk, GetPropertyValue<long>(result!, "Uk"));
            Assert.AreEqual(selfInfo.BdUk, GetPropertyValue<string>(result!, "BdUk"));
            await serverTask;
            return 0;
        });
    }

    [TestMethod]
    public async Task BlcpJsonRpc_CoversSuccessPreservedFieldsRpcFailuresAndPayloadFailures()
    {
        var sender = new BlcpChatroomSender();

        await WithAuthenticatedTlsStreamsAsync(async (clientStream, serverStream) =>
        {
            var serverTask = Task.Run(async () =>
            {
                var request = await ReadBlcpFrameAsync(serverStream, CancellationToken.None);
                var json = JObject.Parse(Encoding.UTF8.GetString(request.LcmBody));

                Assert.AreEqual("hello", json["message"]?.Value<string>());
                Assert.IsGreaterThan(0L, json["client_logid"]?.Value<long>() ?? 0);
                Assert.AreEqual("{\"rpc_retry_time\":0}", json["rpc"]?.Value<string>());

                await WriteBlcpFrameAsync(serverStream,
                    CreateRpcResponseBody(),
                    Encoding.UTF8.GetBytes("{\"value\":1}"),
                    CancellationToken.None);
            });

            var response = (JObject)(await InvokeInstanceTaskWithResultAsync(sender, "SendJsonRpcAsync", clientStream,
                3L, 185L, new { message = "hello" }, CancellationToken.None, "err_code"))!;

            Assert.AreEqual(1, response["value"]?.Value<int>());
            await serverTask;
            return 0;
        });

        await WithAuthenticatedTlsStreamsAsync(async (clientStream, serverStream) =>
        {
            var payload = new JObject
            {
                ["client_logid"] = 123456, ["rpc"] = "{\"rpc_retry_time\":9}", ["message"] = "preserved"
            };

            var serverTask = Task.Run(async () =>
            {
                var request = await ReadBlcpFrameAsync(serverStream, CancellationToken.None);
                var json = JObject.Parse(Encoding.UTF8.GetString(request.LcmBody));

                Assert.AreEqual(123456L, json["client_logid"]?.Value<long>());
                Assert.AreEqual("{\"rpc_retry_time\":9}", json["rpc"]?.Value<string>());

                await WriteBlcpFrameAsync(serverStream,
                    CreateRpcResponseBody(),
                    Encoding.UTF8.GetBytes("{\"errno\":0}"),
                    CancellationToken.None);
            });

            var response = (JObject)(await InvokeInstanceTaskWithResultAsync(sender, "SendJsonRpcAsync", clientStream,
                2L, 50L, payload, CancellationToken.None, "errno"))!;

            Assert.AreEqual(0, response["errno"]?.Value<int>());
            await serverTask;
            return 0;
        });

        await WithAuthenticatedTlsStreamsAsync(async (clientStream, serverStream) =>
        {
            var serverTask = Task.Run(async () =>
            {
                _ = await ReadBlcpFrameAsync(serverStream, CancellationToken.None);
                await WriteBlcpFrameAsync(serverStream,
                    CreateRpcResponseBody(errorText: "rpc failed", errorCode: 0),
                    Encoding.UTF8.GetBytes("{}"),
                    CancellationToken.None);
            });

            var exception = await ThrowsAsync<TiebaProtocolException>(() =>
                InvokeInstanceTaskWithResultAsync(sender, "SendJsonRpcAsync", clientStream, 7L, 9L,
                    new { message = "boom" }, CancellationToken.None, "err_code"));

            StringAssert.Contains(exception.Message, "BLCP RPC 7/9 failed");
            await serverTask;
            return 0;
        });

        await WithAuthenticatedTlsStreamsAsync(async (clientStream, serverStream) =>
        {
            var serverTask = Task.Run(async () =>
            {
                _ = await ReadBlcpFrameAsync(serverStream, CancellationToken.None);
                await WriteBlcpFrameAsync(serverStream,
                    CreateRpcResponseBody(),
                    Encoding.UTF8.GetBytes("{\"errno\":5}"),
                    CancellationToken.None);
            });

            var exception = await ThrowsAsync<TiebaProtocolException>(() =>
                InvokeInstanceTaskWithResultAsync(sender, "SendJsonRpcAsync", clientStream, 2L, 50L,
                    new { message = "boom" }, CancellationToken.None, "errno"));

            StringAssert.Contains(exception.Message, "errno=5");
            await serverTask;
            return 0;
        });
    }

    [TestMethod]
    public async Task BlcpChatroomPayload_CoversFallbackOptionalFieldsAndRobotBranches()
    {
        var sender = new BlcpChatroomSender();
        var account = CreateAccount();
        account.ZId = "zid-1";
        var atData = InvokeStatic<JArray>(typeof(BlcpChatroomSender), "BuildAtData", (IReadOnlyList<long>)[11L, 22L]);
        var forumLevel = new ForumLevelInfo { UserLevel = 9 };
        var vipSelf = new UserInfo
        {
            UserId = 42,
            UserName = "sender",
            NickNameNew = "Sender",
            Portrait = "tb.1.sender",
            Uk = 88,
            IsVip = true,
            GLevel = 12
        };
        var plainSelf = new UserInfo
        {
            UserId = 77,
            UserName = "plain-user",
            Portrait = "tb.1.plain",
            Uk = 99,
            GLevel = 3
        };

        await WithAuthenticatedTlsStreamsAsync(async (clientStream, serverStream) =>
        {
            var serverTask = Task.Run(async () =>
            {
                var request = await ReadBlcpFrameAsync(serverStream, CancellationToken.None);
                var json = JObject.Parse(Encoding.UTF8.GetString(request.LcmBody));
                var content = JObject.Parse(json["content"]!.Value<string>()!);
                var textPayload = JObject.Parse(content["text"]!.Value<string>()!);
                var ext = JObject.Parse(textPayload["ext"]!.Value<string>()!);
                var expectedBduk = InvokeStatic<string>(typeof(BlcpChatroomSender), "GetBdukFromUserId",
                    vipSelf.UserId.ToString());

                Assert.AreEqual(vipSelf.Uk, json["uk"]?.Value<long>());
                Assert.AreEqual(expectedBduk, textPayload["baidu_uk"]?.Value<string>());
                Assert.AreEqual(2, textPayload["at_data"]?.Value<JArray>()?.Count);
                Assert.AreEqual(7, ext["content"]?["robot_params"]?["type"]?.Value<int>());
                Assert.AreEqual("tieba_group_chat", ext["content"]?["robot_params"]?["scene"]?.Value<string>());

                await WriteBlcpFrameAsync(serverStream,
                    CreateRpcResponseBody(),
                    Encoding.UTF8.GetBytes("{}"),
                    CancellationToken.None);
            });

            var loginPayload = CreateLoginPayload(11, 0, string.Empty);
            var result = (bool)(await InvokeInstanceTaskWithResultAsync(sender, "SendChatroomPayloadAsync",
                clientStream,
                account, vipSelf, forumLevel, loginPayload, 12345L, 7356044UL, "hello", atData, 7,
                CancellationToken.None))!;

            Assert.IsFalse(result);
            await serverTask;
            return 0;
        });

        await WithAuthenticatedTlsStreamsAsync(async (clientStream, serverStream) =>
        {
            var serverTask = Task.Run(async () =>
            {
                var request = await ReadBlcpFrameAsync(serverStream, CancellationToken.None);
                var json = JObject.Parse(Encoding.UTF8.GetString(request.LcmBody));
                var content = JObject.Parse(json["content"]!.Value<string>()!);
                var textPayload = JObject.Parse(content["text"]!.Value<string>()!);
                var ext = JObject.Parse(textPayload["ext"]!.Value<string>()!);

                Assert.AreEqual(456L, json["uk"]?.Value<long>());
                Assert.AreEqual("login-bduk", textPayload["baidu_uk"]?.Value<string>());
                Assert.IsNull(textPayload["at_data"]);
                Assert.AreEqual(0, ext["content"]?.Value<JObject>()?.Count);
                Assert.AreEqual("plain-user", textPayload["name"]?.Value<string>());

                await WriteBlcpFrameAsync(serverStream,
                    CreateRpcResponseBody(),
                    Encoding.UTF8.GetBytes("{\"err_code\":0}"),
                    CancellationToken.None);
            });

            var loginPayload = CreateLoginPayload(22, 456, "login-bduk");
            var result = (bool)(await InvokeInstanceTaskWithResultAsync(sender, "SendChatroomPayloadAsync",
                clientStream,
                account, plainSelf, forumLevel, loginPayload, 12345L, 7356044UL, "hello", (JArray?)null, -1,
                CancellationToken.None))!;

            Assert.IsTrue(result);
            await serverTask;
            return 0;
        });
    }


    private static T InvokeStatic<T>(Type type, string name, params object[] args)
    {
        var method = GetMethod(type, name, BindingFlags.NonPublic | BindingFlags.Static);
        return (T)method.Invoke(null, args)!;
    }

    private static Task InvokeStaticTask(Type type, string name, params object[] args)
    {
        var method = GetMethod(type, name, BindingFlags.NonPublic | BindingFlags.Static);
        return (Task)method.Invoke(null, args)!;
    }

    private static async Task<object?> InvokeStaticTaskWithResultAsync(Type type, string name, params object[] args)
    {
        var method = GetMethod(type, name, BindingFlags.NonPublic | BindingFlags.Static);
        var task = (Task)method.Invoke(null, args)!;
        await task;
        return task.GetType().GetProperty("Result")?.GetValue(task);
    }

    private static async Task InvokeInstanceTaskAsync(object target, string name, params object[] args)
    {
        var method = GetMethod(target.GetType(), name, BindingFlags.NonPublic | BindingFlags.Instance);
        await (Task)method.Invoke(target, args)!;
    }

    private static async Task<object?> InvokeInstanceTaskWithResultAsync(object target, string name,
        params object[] args)
    {
        var method = GetMethod(target.GetType(), name, BindingFlags.NonPublic | BindingFlags.Instance);
        var task = (Task)method.Invoke(target, args)!;
        await task;
        return task.GetType().GetProperty("Result")?.GetValue(task);
    }

    private static MethodInfo GetMethod(Type type, string name, BindingFlags bindingFlags)
    {
        return type.GetMethod(name, bindingFlags)
               ?? throw new InvalidOperationException($"Method '{name}' not found on {type.FullName}.");
    }

    private static object CreateLoginPayload(long triggerId, long uk, string bdUk)
    {
        var type = typeof(BlcpChatroomSender).GetNestedType("LoginPayload", BindingFlags.NonPublic)
                   ?? throw new InvalidOperationException("LoginPayload type not found.");
        return Activator.CreateInstance(type, triggerId, uk, bdUk)
               ?? throw new InvalidOperationException("Unable to create LoginPayload.");
    }

    private static Account CreateAccount()
    {
        return new Account(new string('b', 192), new string('s', 64))
        {
            SampleId = "sample-1", ClientId = "client-1", CuidGalaxy2 = "123456", C3Aid = "c3-aid"
        };
    }

    private static async Task<T> WithAuthenticatedTlsStreamsAsync<T>(Func<SslStream, SslStream, Task<T>> action)
    {
        var certificate = CreateServerCertificate();
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        try
        {
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            using var client = new TcpClient(AddressFamily.InterNetwork);
            var acceptTask = listener.AcceptTcpClientAsync();
            await client.ConnectAsync(IPAddress.Loopback, port);
            using var server = await acceptTask;
            using var clientStream = new SslStream(client.GetStream(), false,
                static (_, _, _, _) => true);
            using var serverStream = new SslStream(server.GetStream(), false);

            void AbortHandshake()
            {
                clientStream.Dispose();
                serverStream.Dispose();
            }

            var clientAuthTask = clientStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = "localhost",
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck
            });
            var serverAuthTask = serverStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
            {
                ServerCertificate = certificate,
                ClientCertificateRequired = false,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck
            });

            var abortHandshake = (Action)AbortHandshake;

            _ = clientAuthTask.ContinueWith(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                    abortHandshake();
            }, TaskContinuationOptions.ExecuteSynchronously);

            _ = serverAuthTask.ContinueWith(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                    abortHandshake();
            }, TaskContinuationOptions.ExecuteSynchronously);

            await Task.WhenAll(clientAuthTask, serverAuthTask);

            return await action(clientStream, serverStream);
        }
        finally
        {
            listener.Stop();
            certificate.Dispose();
        }
    }

    private static X509Certificate2 CreateServerCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
        var eku = new OidCollection { new Oid("1.3.6.1.5.5.7.3.1", "Server Authentication") };
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(eku, false));
        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName("localhost");
        san.AddIpAddress(IPAddress.Loopback);
        request.CertificateExtensions.Add(san.Build());
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        using var certificate =
            request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        return new X509Certificate2(certificate.Export(X509ContentType.Pfx), (string?)null,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
    }

    private static async Task WriteBytesAsync(Stream stream, byte[] payload, CancellationToken cancellationToken)
    {
        await stream.WriteAsync(payload, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private static async Task WriteBlcpFrameAsync(Stream stream, byte[] rpcBody, byte[] lcmBody,
        CancellationToken cancellationToken)
    {
        var header = new byte[12];
        header[0] = (byte)'l';
        header[1] = (byte)'c';
        header[2] = (byte)'p';
        header[3] = 1;
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4, 4), rpcBody.Length + lcmBody.Length);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(8, 4), rpcBody.Length);

        await stream.WriteAsync(header, cancellationToken);
        await stream.WriteAsync(rpcBody, cancellationToken);
        await stream.WriteAsync(lcmBody, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private static async Task<BlcpFrame> ReadBlcpFrameAsync(Stream stream, CancellationToken cancellationToken)
    {
        var prefix = new byte[4];
        await ReadExactAsync(stream, prefix, cancellationToken);

        var lengths = new byte[8];
        await ReadExactAsync(stream, lengths, cancellationToken);
        var totalLength = BinaryPrimitives.ReadInt32BigEndian(lengths.AsSpan(0, 4));
        var rpcLength = BinaryPrimitives.ReadInt32BigEndian(lengths.AsSpan(4, 4));
        var payload = new byte[totalLength];
        await ReadExactAsync(stream, payload, cancellationToken);

        return new BlcpFrame(
            RpcMeta.Parser.ParseFrom(payload.AsSpan(0, rpcLength).ToArray()),
            payload.AsSpan(rpcLength).ToArray());
    }

    private static async Task ReadExactAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var read = 0;
        while (read < buffer.Length)
        {
            var length = await stream.ReadAsync(buffer.AsMemory(read, buffer.Length - read), cancellationToken);
            if (length == 0)
                throw new EndOfStreamException("TLS test stream closed unexpectedly.");
            read += length;
        }
    }

    private static byte[] CreateRpcResponseBody(int compressType = 0, string errorText = "success", int errorCode = 0)
    {
        return new RpcMeta
        {
            Response = new RpcResponseMeta { ErrorCode = errorCode, ErrorText = errorText },
            CompressType = compressType
        }.ToByteArray();
    }

    private static byte[] CompressGzip(byte[] payload)
    {
        using var target = new MemoryStream();
        using (var gzip = new GZipStream(target, CompressionLevel.SmallestSize, true))
        {
            gzip.Write(payload);
        }

        return target.ToArray();
    }


    private static object GetTupleItem(object tuple, string fieldName)
    {
        return tuple.GetType().GetField(fieldName)?.GetValue(tuple)
               ?? throw new InvalidOperationException($"Tuple field '{fieldName}' was not found.");
    }

    private static T GetPropertyValue<T>(object instance, string propertyName)
    {
        return (T)(instance.GetType().GetProperty(propertyName)?.GetValue(instance)
                   ?? throw new InvalidOperationException($"Property '{propertyName}' was not found."));
    }

    private sealed record BlcpFrame(RpcMeta RpcMeta, byte[] LcmBody);

    private static TException Throws<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException exception)
        {
            return exception;
        }

        Assert.Fail($"Expected exception of type {typeof(TException).Name} was not thrown.");
        throw new InvalidOperationException();
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
