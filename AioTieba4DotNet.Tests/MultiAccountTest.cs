using System.Linq;
using System.Threading.Tasks;
using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests;

[TestClass]
public class MultiAccountTest
{
    private static string MockBduss(string seed)
    {
        return seed.PadRight(192, '0');
    }

    private static string MockStoken(string seed)
    {
        return seed.PadRight(64, '0');
    }

    [TestMethod]
    public void TestMultiAccountFactory()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAioTiebaClient();
        var sp = services.BuildServiceProvider();

        var factory = sp.GetRequiredService<ITiebaClientFactory>();

        var bduss1 = MockBduss("bduss1");
        var bduss2 = MockBduss("bduss2");
        var client1 = factory.CreateClient(bduss1, MockStoken("st1"));
        var client2 = factory.CreateClient(bduss2, MockStoken("st2"));

        Assert.IsNotNull(client1.HttpCore.Account);
        Assert.IsNotNull(client2.HttpCore.Account);
        Assert.AreNotEqual(client1.HttpCore.Account.Bduss, client2.HttpCore.Account.Bduss);
        Assert.AreEqual(bduss1, client1.HttpCore.Account.Bduss);
        Assert.AreEqual(bduss2, client2.HttpCore.Account.Bduss);

        // 验证 WS 隔离性
        Assert.IsNotNull(client1.WsCore);
        Assert.IsNotNull(client2.WsCore);
        Assert.AreNotSame(client1.WsCore, client2.WsCore, "Each client must have a separate WsCore instance");
        Assert.AreEqual(bduss1, client1.WsCore.Account?.Bduss);
        Assert.AreEqual(bduss2, client2.WsCore.Account?.Bduss);
    }

    [TestMethod]
    public async Task TestConcurrentAccountAccessAsync()
    {
        var account = new Account(MockBduss("test"), MockStoken("test"));

        var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(() =>
        {
            _ = account.AndroidId;
            _ = account.Uuid;
            _ = account.AesEcbCipher;
            _ = account.Cuid;
            _ = account.CuidGalaxy2;
        }));

        await Task.WhenAll(tasks);

        Assert.IsNotNull(account.AndroidId);
        Assert.IsNotNull(account.Uuid);
        Assert.IsNotNull(account.AesEcbCipher);
        Assert.IsNotNull(account.Cuid);
        Assert.IsNotNull(account.CuidGalaxy2);
    }
}
