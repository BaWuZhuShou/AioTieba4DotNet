using System;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AioTieba4DotNet.Core;

namespace AioTieba4DotNet.Tests.Core;

[TestClass]
public class AccountTest
{
    [TestMethod]
    public void TestAccountFieldsGeneration()
    {
        var account = new Account();

        // 验证 AndroidId 格式：16位小写16进制
        Assert.AreEqual(16, account.AndroidId.Length);
        Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(account.AndroidId, "^[0-9a-f]{16}$"));

        // 验证 Uuid 格式：标准 UUID
        Assert.IsTrue(Guid.TryParse(account.Uuid, out _));
        Assert.IsTrue(account.Uuid.Contains("-"));

        // 验证 Cuid 格式
        Assert.AreEqual("baidutiebaapp" + account.Uuid, account.Cuid);

        // 验证 CuidGalaxy2 逻辑
        var expectedCuidGalaxy2 = TbCrypto.CuidGalaxy2(account.AndroidId);
        Assert.AreEqual(expectedCuidGalaxy2, account.CuidGalaxy2);

        // 验证 AesEcbSecKey 长度
        Assert.AreEqual(31, account.AesEcbSecKey.Length);

        // 验证 AesCbcSecKey 长度
        Assert.AreEqual(16, account.AesCbcSecKey.Length);
    }

    [TestMethod]
    public void TestAccountConsistencyWithPredefinedValues()
    {
        var account = new Account();

        // 测试 CuidGalaxy2 相关数据 (数据来源于 TBCryptoTest)
        account.AndroidId = "6723280942424234";
        Assert.AreEqual("7A906FF80FFA1FCDF93F8CBEFEC546BA|VNHO3C5IV", account.CuidGalaxy2);

        // 测试 C3Aid 相关数据 (数据来源于 TBCryptoTest)
        // 注意：修改 AndroidId 后，由于 C3Aid 是懒加载的，如果已经生成过可能不会重新生成。
        // 但在这个测试中我们还没访问过 C3Aid。为了保险起见，我们重新创建一个 account 或者使用反射清理缓存。
        // 考虑到 Account 类内部逻辑，我们直接新建。

        var account2 = new Account
        {
            AndroidId = "6723280942DS4234",
            Uuid = "d5992777-6dd1-40c7-84e4-489332c41a81"
        };
        Assert.AreEqual("A00-YOMYUVSSXRCD6Y473WPJ7SMQDAIQLEYU-3NI4Y2N5", account2.C3Aid);

        // 验证 Cuid
        Assert.AreEqual("baidutiebaapp" + account2.Uuid, account2.Cuid);
    }

    [TestMethod]
    public void TestAesEcbCipher()
    {
        var account = new Account();
        // 设置固定的 key 方便验证
        var key = new byte[31];
        for(int i=0; i<31; i++) key[i] = (byte)i;

        // 使用反射设置私有字段 _aesEcbSecKey (因为没有 Setter)
        var field = typeof(Account).GetField("_aesEcbSecKey", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field?.SetValue(account, key);

        var cipher = account.AesEcbCipher;
        Assert.IsNotNull(cipher);
        Assert.AreEqual(CipherMode.ECB, cipher.Mode);

        // 验证 Key 是否是通过 PBKDF2 生成的
        var expectedKey = Rfc2898DeriveBytes.Pbkdf2(key, (byte[])[0xa4, 0x0b, 0xc8, 0x34, 0xd6, 0x95, 0xf3, 0x13], 5, HashAlgorithmName.SHA1, 32);
        CollectionAssert.AreEqual(expectedKey, cipher.Key);
    }

    [TestMethod]
    public void TestBdussValidation()
    {
        var validBduss = new string('A', 192);
        var account = new Account(validBduss);
        Assert.AreEqual(validBduss, account.Bduss);

        try
        {
            _ = new Account(new string('A', 191));
            Assert.Fail("Should throw ArgumentException");
        }
        catch (ArgumentException)
        {
        }
    }

    [TestMethod]
    public void TestStokenValidation()
    {
        var validStoken = new string('A', 64);
        var account = new Account("", validStoken);
        Assert.AreEqual(validStoken, account.Stoken);

        try
        {
            _ = new Account("", new string('A', 63));
            Assert.Fail("Should throw ArgumentException");
        }
        catch (ArgumentException)
        {
        }
    }
}
