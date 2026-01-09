using System;
using System.Text;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AioTieba4DotNet.Core;

namespace AioTieba4DotNet.Tests.Core;

[TestClass]
[TestSubject(typeof(Rc4))]
public class Rc4Test
{

    [TestMethod]
    public void Test()
    {
        var rc4 = new Rc4(Encoding.UTF8.GetBytes("f00c29de98c67b3866a9a816efde42eb"));
        var crypt = rc4.Crypt([24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24]);
        Assert.AreEqual(BitConverter.ToString(crypt).Replace("-", "").ToLower(), "54d8e53a098b07a0461f196fe1e7f3bf");
    }

    [TestMethod]
    public void TestBase64()
    {
        var rc4 = new Rc4(Encoding.UTF8.GetBytes("f00c29de98c67b3866a9a816efde42eb"));
        var crypt = rc4.Crypt([24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24]);
        Console.WriteLine(Convert.ToBase64String(crypt));
    }
}