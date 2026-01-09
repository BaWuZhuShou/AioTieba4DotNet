using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GetFidApi = AioTieba4DotNet.Api.GetFid.GetFid;

namespace AioTieba4DotNet.Tests.Api.GetFid;

[TestClass]
[TestSubject(typeof(GetFidApi))]
public class GetFidTest : TestBase
{
    [TestMethod]
    public async Task TestRequest()
    {
        var getFid = new GetFidApi(HttpCore);
        var fid = await getFid.RequestAsync("DNF吧");
        Assert.IsGreaterThan<ulong>(0, fid);
        Console.WriteLine($"DNF吧的FID: {fid}");
    }
}