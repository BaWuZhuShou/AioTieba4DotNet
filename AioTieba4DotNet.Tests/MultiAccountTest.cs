using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests;

[TestClass]
public class MultiAccountTest
{
    [TestMethod]
    public void TestMultiAccountFactory()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAioTiebaClient();
        var sp = services.BuildServiceProvider();

        var factory = sp.GetRequiredService<ITiebaClientFactory>();

        var client1 = factory.CreateClient("bduss1", "stoken1");
        var client2 = factory.CreateClient("bduss2", "stoken2");

        Assert.IsNotNull(client1.HttpCore.Account);
        Assert.IsNotNull(client2.HttpCore.Account);
        Assert.AreNotEqual(client1.HttpCore.Account.Bduss, client2.HttpCore.Account.Bduss);
        Assert.AreEqual("bduss1", client1.HttpCore.Account.Bduss);
        Assert.AreEqual("bduss2", client2.HttpCore.Account.Bduss);

        // 验证 WS 隔离性
        Assert.IsNotNull(client1.WsCore);
        Assert.IsNotNull(client2.WsCore);
        Assert.AreNotSame(client1.WsCore, client2.WsCore, "Each client must have a separate WsCore instance");
        Assert.AreEqual("bduss1", client1.WsCore.Account?.Bduss);
        Assert.AreEqual("bduss2", client2.WsCore.Account?.Bduss);
    }

    [TestMethod]
    public async Task TestConcurrentAccountAccess()
    {
        var account = new Account("test", "test");
        var tasks = new List<Task>();

        for (var i = 0; i < 100; i++)
            tasks.Add(Task.Run(() =>
            {
                var id = account.AndroidId;
                var uuid = account.Uuid;
                var cipher = account.AesEcbCipher;
                var cuid = account.Cuid;
                var cuidG2 = account.CuidGalaxy2;
            }));

        await Task.WhenAll(tasks);

        Assert.IsNotNull(account.AndroidId);
        Assert.IsNotNull(account.Uuid);
        Assert.IsNotNull(account.AesEcbCipher);
        Assert.IsNotNull(account.Cuid);
        Assert.IsNotNull(account.CuidGalaxy2);
    }
}
