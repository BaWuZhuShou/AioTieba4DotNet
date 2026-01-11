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
        EnsureAuthenticated();

        var syncApi = new AioTieba4DotNet.Api.Sync.Sync(HttpCore);
        try
        {
            await syncApi.RequestAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
