using System;
using System.Text;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AioTieba4DotNet.Core;

namespace AioTieba4DotNet.Tests.Core;

[TestClass]
[TestSubject(typeof(Utils))]
public class UtilsTest
{

    [TestMethod]
    public void TestGenerateAndroidId()
    {
        var generateAndroidId = Utils.GenerateAndroidId();
        Console.WriteLine(generateAndroidId);
    }

    [TestMethod]
    public void TestApplyPkcs7Padding()
    {
        var bytes = Encoding.UTF8.GetBytes("11111111111111112222222222");
        var applyPkcs7Padding = Utils.ApplyPkcs7Padding(bytes,32);
        Console.WriteLine(BitConverter.ToString(applyPkcs7Padding).Replace("-", string.Empty));
    }
}