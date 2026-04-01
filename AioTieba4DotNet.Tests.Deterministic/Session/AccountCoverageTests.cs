#nullable enable
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Session;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Session;

[TestClass]
public sealed class AccountCoverageTests
{
    [TestMethod]
    public void Account_GeneratedIdentifiers_AndCryptoHelpers_AreCachedAndWellShaped()
    {
        var account = new Account(new string('b', 192), new string('s', 64));

        var androidId = account.AndroidId;
        var uuid = account.Uuid;
        var cuid = account.Cuid;
        var cuidGalaxy2 = account.CuidGalaxy2;
        var c3Aid = account.C3Aid;
        var ecbKey = account.AesEcbSecKey;
        var ecbKeyAgain = account.AesEcbSecKey;
        var ecbCipher = account.AesEcbCipher;
        var ecbCipherAgain = account.AesEcbCipher;
        var cbcKey = account.AesCbcSecKey;
        var cbcCipher = account.AesCbcCipher;
        var cbcCipherAgain = account.AesCbcCipher;

        Assert.AreEqual(16, androidId.Length);
        Assert.AreEqual(uuid, account.Uuid);
        StringAssert.StartsWith(cuid, "baidutiebaapp");
        Assert.IsFalse(string.IsNullOrWhiteSpace(cuidGalaxy2));
        Assert.IsFalse(string.IsNullOrWhiteSpace(c3Aid));
        Assert.AreSame(ecbKey, ecbKeyAgain);
        Assert.AreEqual(31, ecbKey.Length);
        Assert.AreSame(ecbCipher, ecbCipherAgain);
        Assert.AreEqual(CipherMode.ECB, ecbCipher.Mode);
        Assert.AreEqual(PaddingMode.PKCS7, ecbCipher.Padding);
        Assert.AreEqual(16, cbcKey.Length);
        Assert.AreSame(cbcCipher, cbcCipherAgain);
        Assert.AreEqual(CipherMode.CBC, cbcCipher!.Mode);
        Assert.AreEqual(PaddingMode.None, cbcCipher.Padding);
        CollectionAssert.AreEqual(new byte[16], cbcCipher.IV);
    }

    [TestMethod]
    public void Account_ExplicitSetters_OverrideGeneratedValues()
    {
        var account = new Account();
        var customEcbKey = new byte[31];
        var customCbcKey = new byte[16];

        account.AndroidId = "0123456789abcdef";
        account.Uuid = "00000000-0000-0000-0000-000000000000";
        account.Cuid = "custom-cuid";
        account.CuidGalaxy2 = "custom-galaxy2";
        account.C3Aid = "custom-c3aid";
        account.AesCbcSecKey = customCbcKey;

        Assert.AreEqual("0123456789abcdef", account.AndroidId);
        Assert.AreEqual("00000000-0000-0000-0000-000000000000", account.Uuid);
        Assert.AreEqual("custom-cuid", account.Cuid);
        Assert.AreEqual("custom-galaxy2", account.CuidGalaxy2);
        Assert.AreEqual("custom-c3aid", account.C3Aid);
        CollectionAssert.AreEqual(customCbcKey, account.AesCbcSecKey);

        var ecbField = typeof(Account).GetField("_aesEcbSecKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        ecbField!.SetValue(account, customEcbKey);

        CollectionAssert.AreEqual(customEcbKey, account.AesEcbSecKey);
    }

    [TestMethod]
    public void Account_NullCacheResets_RegenerateIdentifiersAndKeys()
    {
        var account = new Account(new string('b', 192), new string('s', 64))
        {
            AndroidId = null!,
            Uuid = null!,
            Cuid = null!,
            CuidGalaxy2 = null!,
            C3Aid = null!,
            AesCbcSecKey = null!
        };

        Assert.AreEqual(16, account.AndroidId.Length);
        Assert.AreEqual(36, account.Uuid.Length);
        StringAssert.StartsWith(account.Cuid, "baidutiebaapp");
        Assert.IsFalse(string.IsNullOrWhiteSpace(account.CuidGalaxy2));
        Assert.IsFalse(string.IsNullOrWhiteSpace(account.C3Aid));
        Assert.AreEqual(16, account.AesCbcSecKey.Length);
    }

    [TestMethod]
    public void Account_RejectsInvalidCredentialLengths()
    {
        var bduss = Throws<ArgumentException>(() => new Account("short", string.Empty));
        var stoken = Throws<ArgumentException>(() => new Account(string.Empty, "short"));

        StringAssert.Contains(bduss.Message, "BDUSS length should be 192");
        StringAssert.Contains(stoken.Message, "STOKEN length should be 64");
    }

    [TestMethod]
    public async Task Account_LazyProperties_ReturnSameInstances_UnderConcurrentInitialization()
    {
        var account = new Account(new string('b', 192), new string('s', 64));

        var androidIds = await ReadConcurrentlyAsync(() => account.AndroidId);
        var uuids = await ReadConcurrentlyAsync(() => account.Uuid);
        var cuids = await ReadConcurrentlyAsync(() => account.Cuid);
        var c3Aids = await ReadConcurrentlyAsync(() => account.C3Aid!);
        var ecbKeys = await ReadConcurrentlyAsync(() => account.AesEcbSecKey);
        var ecbCiphers = await ReadConcurrentlyAsync(() => account.AesEcbCipher);
        var cbcKeys = await ReadConcurrentlyAsync(() => account.AesCbcSecKey);
        var cbcCiphers = await ReadConcurrentlyAsync(() => account.AesCbcCipher!);

        AssertAllSameReference(androidIds);
        AssertAllSameReference(uuids);
        AssertAllSameReference(cuids);
        AssertAllSameReference(c3Aids);
        AssertAllSameReference(ecbKeys);
        AssertAllSameReference(ecbCiphers);
        AssertAllSameReference(cbcKeys);
        AssertAllSameReference(cbcCiphers);
    }

    [TestMethod]
    public void Account_PreseededFields_ReturnExistingValuesWithoutRegeneration()
    {
        var account = new Account();
        var uuidField = typeof(Account).GetField("_uuid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var cuidField = typeof(Account).GetField("_cuid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var ecbKeyField = typeof(Account).GetField("_aesEcbSecKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var cbcKeyField = typeof(Account).GetField("_aesCbcSecKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var cbcCipherField = typeof(Account).GetField("_aesCbcCipher", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var seededEcbKey = new byte[31];
        var seededCbcKey = new byte[16];
        using var seededCipher = Aes.Create();

        uuidField!.SetValue(account, "seeded-uuid");
        cuidField!.SetValue(account, "seeded-cuid");
        ecbKeyField!.SetValue(account, seededEcbKey);
        cbcKeyField!.SetValue(account, seededCbcKey);
        cbcCipherField!.SetValue(account, seededCipher);

        Assert.AreEqual("seeded-uuid", account.Uuid);
        Assert.AreEqual("seeded-cuid", account.Cuid);
        CollectionAssert.AreEqual(seededEcbKey, account.AesEcbSecKey);
        CollectionAssert.AreEqual(seededCbcKey, account.AesCbcSecKey);
        Assert.AreSame(seededCipher, account.AesCbcCipher);
    }

    [TestMethod]
    public void TbCrypto_RejectsInvalidLengths_AndEncodesRemainderBits()
    {
        var cryptoType = typeof(Account).Assembly.GetType("AioTieba4DotNet.Internal.TbCrypto", throwOnError: true)!;
        var cuidGalaxy2 = cryptoType.GetMethod("CuidGalaxy2", BindingFlags.Public | BindingFlags.Static)!;
        var c3Aid = cryptoType.GetMethod("C3Aid", BindingFlags.Public | BindingFlags.Static)!;
        var base32Encode = (Base32EncodeDelegate)cryptoType.GetMethod("Base32Encode",
            BindingFlags.NonPublic | BindingFlags.Static)!.CreateDelegate(typeof(Base32EncodeDelegate));

        var invalidCuidGalaxy2 = Throws<TargetInvocationException>(() => cuidGalaxy2.Invoke(null, [new string('a', 15)]));
        var invalidC3AidAndroid = Throws<TargetInvocationException>(() => c3Aid.Invoke(null,
            [new string('a', 15), new string('b', 36)]));
        var invalidC3AidUuid = Throws<TargetInvocationException>(() => c3Aid.Invoke(null,
            [new string('a', 16), new string('b', 35)]));
        Span<byte> remainderInput = stackalloc byte[1];
        remainderInput[0] = 0xFF;
        var encoded = base32Encode(remainderInput);

        Assert.AreEqual("Invalid size of android_id. Expected 16, got 15", invalidCuidGalaxy2.InnerException?.Message);
        Assert.AreEqual("Invalid size of android_id. Expected 16, got 15", invalidC3AidAndroid.InnerException?.Message);
        Assert.AreEqual("Invalid size of uuid. Expected 36, got 35", invalidC3AidUuid.InnerException?.Message);
        Assert.AreEqual("74", encoded);
    }

    [TestMethod]
    public void Account_Getters_UseInnerCachedBranch_WhenAnotherThreadSeedsValuesWhileTheLockIsHeld()
    {
        var account = new Account();
        var seededAndroidId = new string('a', 16);
        var seededUuid = new string('u', 36);
        var seededCuid = "seeded-cuid";
        var seededCuidGalaxy2 = "seeded-galaxy2";
        var seededC3Aid = "seeded-c3aid";
        var seededEcbKey = new byte[31];
        using var seededEcbCipher = Aes.Create();
        var seededCbcKey = new byte[16];
        using var seededCipher = Aes.Create();

        Assert.AreSame(seededAndroidId, ExerciseInnerCachedBranch(account, "_androidId", static value => value.AndroidId, seededAndroidId));
        Assert.AreSame(seededUuid, ExerciseInnerCachedBranch(account, "_uuid", static value => value.Uuid, seededUuid));
        Assert.AreSame(seededCuid, ExerciseInnerCachedBranch(account, "_cuid", static value => value.Cuid, seededCuid));
        Assert.AreSame(seededCuidGalaxy2,
            ExerciseInnerCachedBranch(account, "_cuidGalaxy2", static value => value.CuidGalaxy2, seededCuidGalaxy2));
        Assert.AreSame(seededC3Aid, ExerciseInnerCachedBranch(account, "_c3Aid", static value => value.C3Aid!, seededC3Aid));
        Assert.AreSame(seededEcbKey,
            ExerciseInnerCachedBranch(account, "_aesEcbSecKey", static value => value.AesEcbSecKey, seededEcbKey));
        Assert.AreSame(seededEcbCipher,
            ExerciseInnerCachedBranch(account, "_aesEcbCipher", static value => value.AesEcbCipher, seededEcbCipher));
        Assert.AreSame(seededCbcKey,
            ExerciseInnerCachedBranch(account, "_aesCbcSecKey", static value => value.AesCbcSecKey, seededCbcKey));
        Assert.AreSame(seededCipher,
            ExerciseInnerCachedBranch(account, "_aesCbcCipher", static value => value.AesCbcCipher!, seededCipher));
    }

    private static async Task<T[]> ReadConcurrentlyAsync<T>(Func<T> read)
        where T : class
    {
        using var start = new ManualResetEventSlim(false);
        var tasks = Enumerable.Range(0, 16)
            .Select(_ => Task.Run(() =>
            {
                start.Wait();
                return read();
            }))
            .ToArray();

        start.Set();
        return await Task.WhenAll(tasks);
    }

    private static void AssertAllSameReference<T>(T[] values)
        where T : class
    {
        Assert.IsTrue(values.Length > 1);
        for (var index = 1; index < values.Length; index++)
            Assert.AreSame(values[0], values[index]);
    }

    private static T ExerciseInnerCachedBranch<T>(Account account, string fieldName, Func<Account, T> getter, T seededValue)
        where T : class
    {
        var field = typeof(Account).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException($"Missing field '{fieldName}'.");
        var lockField = typeof(Account).GetField("_lock", BindingFlags.Instance | BindingFlags.NonPublic)
                        ?? throw new InvalidOperationException("Missing _lock field.");
        var lockObject = lockField.GetValue(account) ?? throw new InvalidOperationException("Missing account lock object.");

        field.SetValue(account, null);

        T? result = null;
        Exception? failure = null;
        using var started = new ManualResetEventSlim(false);
        var thread = new Thread(() =>
        {
            started.Set();
            try
            {
                result = getter(account);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        lock (lockObject)
        {
            thread.Start();
            Assert.IsTrue(started.Wait(TimeSpan.FromSeconds(1)), $"Timed out starting getter for {fieldName}.");
            Assert.IsTrue(SpinWait.SpinUntil(() => (thread.ThreadState & ThreadState.WaitSleepJoin) != 0,
                TimeSpan.FromSeconds(1)), $"Timed out blocking getter for {fieldName} on the account lock.");
            field.SetValue(account, seededValue);
        }

        thread.Join();
        if (failure is not null)
            throw new AssertFailedException($"Getter '{fieldName}' threw unexpectedly: {failure}");

        Assert.IsNotNull(result, $"Getter '{fieldName}' did not produce a value.");
        return result!;
    }

    private delegate string Base32EncodeDelegate(Span<byte> input);

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
}
