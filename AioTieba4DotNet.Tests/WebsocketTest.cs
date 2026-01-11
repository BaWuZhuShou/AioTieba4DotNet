using AioTieba4DotNet.Core;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests;

[TestClass]
public class WebsocketTest
{
    [TestMethod]
    public void TestWsReqSerialization()
    {
        var req = new WSReq
        {
            Cmd = 1, ReqId = 123, Payload = new WSReq.Types.Payload { Data = ByteString.CopyFromUtf8("test") }
        };

        var data = req.ToByteArray();
        var parsed = WSReq.Parser.ParseFrom(data);

        Assert.AreEqual(req.Cmd, parsed.Cmd);
        Assert.AreEqual(req.ReqId, parsed.ReqId);
        Assert.AreEqual(req.Payload.Data, parsed.Payload.Data);
    }

    [TestMethod]
    public void TestRc4Crypt()
    {
        var key = "12345678"u8.ToArray();
        var rc4 = new Rc4(key);
        var data = "hello world"u8.ToArray();

        var encrypted = rc4.Crypt(data);

        var rc4_2 = new Rc4(key);
        var decrypted = rc4_2.Crypt(encrypted);

        CollectionAssert.AreEqual(data, decrypted);
    }
}
