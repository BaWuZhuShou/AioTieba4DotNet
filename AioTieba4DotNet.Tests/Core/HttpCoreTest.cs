using System.Collections.Generic;
using System.Linq;
using AioTieba4DotNet.Core;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Core;

[TestClass]
[TestSubject(typeof(HttpCore))]
public class HttpCoreTest
{
    [TestMethod]
    public void TestSign()
    {
        var items = new List<KeyValuePair<string, string>> { new("key1", "value1"), new("key2", "12345") };
        var result = HttpCore.Sign(items);
        Assert.AreEqual("9732aa652304b3770aba8902323a05a7", result.Last().Value);
    }
}
