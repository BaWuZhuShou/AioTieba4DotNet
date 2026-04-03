using System;
using System.Linq;
using System.Text;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Core;

[TestClass]
[TestSubject(typeof(Utils))]
public class UtilsTest
{
    [TestMethod]
    public void TestGenerateAndroidId()
    {
        var generateAndroidId = Utils.GenerateAndroidId();
        Assert.AreEqual(16, generateAndroidId.Length);
        Assert.IsTrue(generateAndroidId.All(static c => char.IsDigit(c) || (c >= 'a' && c <= 'f')));
    }

    [TestMethod]
    public void TestApplyPkcs7Padding()
    {
        var bytes = Encoding.UTF8.GetBytes("11111111111111112222222222");
        var applyPkcs7Padding = Utils.ApplyPkcs7Padding(bytes, 32);
        Assert.AreEqual("3131313131313131313131313131313132323232323232323232060606060606",
            BitConverter.ToString(applyPkcs7Padding).Replace("-", string.Empty));
    }
}
