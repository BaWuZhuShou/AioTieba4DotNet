using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.Sync;

[TestClass]
[TestSubject(typeof(AioTieba4DotNet.Api.Sync.Sync))]
public class SyncTest : TestBase
{
    [TestMethod]
    public async Task TestRequestAsync()
    {
        if (!IsAuthenticated)
        {
            Assert.Inconclusive("未设置 BDUSS，跳过 Sync 测试");
            return;
        }

        var syncApi = new AioTieba4DotNet.Api.Sync.Sync(HttpCore);
        var result = await syncApi.RequestAsync();
        Assert.IsNotNull(result);
        Console.WriteLine(result);
    }
}