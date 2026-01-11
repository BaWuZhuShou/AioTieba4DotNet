using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AioTieba4DotNet.Core;

namespace AioTieba4DotNet.Tests.Core;

[TestClass]
[TestSubject(typeof(TbCrypto))]
public class TbCryptoTest
{
    [TestMethod]
    public void TestCuidGalaxy2()
    {
        var cuidGalaxy2 = TbCrypto.CuidGalaxy2("6723280942424234");
        Assert.AreEqual("7A906FF80FFA1FCDF93F8CBEFEC546BA|VNHO3C5IV", cuidGalaxy2);
    }

    [TestMethod]
    public void TestC3Aid()
    {
        var g = "d5992777-6dd1-40c7-84e4-489332c41a81";
        var androidId = "6723280942DS4234";
        var c3Aid = TbCrypto.C3Aid(androidId, g);
        Assert.AreEqual("A00-YOMYUVSSXRCD6Y473WPJ7SMQDAIQLEYU-3NI4Y2N5", c3Aid);
    }
}
