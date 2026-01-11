using AioTieba4DotNet.Core;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests;

[TestClass]
public class WsCoreTest
{
    [TestMethod]
    public void TestPackAndParse()
    {
        var account = new Account();
        var wsCore = new WebsocketCore(account);
        var data = "hello world"u8.ToArray();
        var cmd = 123456;
        var reqId = 7890;

        // 测试加密打包
        var packed = wsCore.PackWsBytes(data, cmd, reqId, true);
        Assert.AreEqual(0x88, packed[0]); // 0x08 | 0x80

        // 测试解包
        var (parsedData, parsedCmd, parsedReqId) = wsCore.ParseWsBytes(packed);

        Assert.AreEqual(cmd, parsedCmd);
        Assert.AreEqual(reqId, parsedReqId);
        CollectionAssert.AreEqual(data, parsedData);
    }

    [TestMethod]
    public void TestPackAndParseNoEncrypt()
    {
        var account = new Account();
        var wsCore = new WebsocketCore(account);
        var data = "hello world"u8.ToArray();
        var cmd = 123456;
        var reqId = 7890;

        // 测试不加密打包
        var packed = wsCore.PackWsBytes(data, cmd, reqId, false);
        Assert.AreEqual(0x08, packed[0]);

        // 测试解包
        var (parsedData, parsedCmd, parsedReqId) = wsCore.ParseWsBytes(packed);

        Assert.AreEqual(cmd, parsedCmd);
        Assert.AreEqual(reqId, parsedReqId);
        CollectionAssert.AreEqual(data, parsedData);
    }
}
