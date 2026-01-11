using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AioTieba4DotNet.Core;

namespace AioTieba4DotNet.Tests.Api.InitZId;

[TestClass]
[TestSubject(typeof(AioTieba4DotNet.Api.InitZId.InitZId))]
public class InitZIdTest : TestBase
{
    [TestMethod]
    public void TestGetMd5Hash()
    {
        var account = new Account
        {
            AndroidId = "5cae590c1ee17ff1",
            Uuid = "6031f0e4-99b0-4c09-a21f-43af72db4426",
            AesCbcSecKey = [24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24, 24]
        };
        var xyus = AioTieba4DotNet.Api.InitZId.InitZId.GetMd5Hash(account.AndroidId + account.Uuid) + "|0";
        var xyusMd5Str = AioTieba4DotNet.Api.InitZId.InitZId.GetMd5Hash(xyus).ToLower();
        var currentTs = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var reqBody = "{\"module_section\":[{\"zid\":\"" + xyus + "\"}]}";
        var reqBodyBytes = Encoding.UTF8.GetBytes(reqBody);
        var reqBodyCompressed = AioTieba4DotNet.Api.InitZId.InitZId.Compress(reqBodyBytes);
        var padding = Utils.ApplyPkcs7Padding(reqBodyCompressed, 16);
        var cryptoTransform = account.AesCbcCipher!.CreateEncryptor(account.AesCbcCipher.Key, account.AesCbcCipher.IV);
        var transformFinalBlock = cryptoTransform.TransformFinalBlock(padding, 0, padding.Length);
        var reqBodyMd5 = MD5.HashData(reqBodyCompressed);
        Console.WriteLine(BitConverter.ToString(reqBodyMd5));
    }

    [TestMethod]
    public async Task TestRequest()
    {
        var initZId = new AioTieba4DotNet.Api.InitZId.InitZId(HttpCore);
        var result = await initZId.RequestAsync();
        Assert.IsNotNull(result);
        Console.WriteLine(result);
    }
}
