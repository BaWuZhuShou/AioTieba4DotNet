using System;
using System.Threading.Tasks;
using AioTieba4DotNet.Testing;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GetFidApi = AioTieba4DotNet.Api.GetFid.GetFid;

namespace AioTieba4DotNet.Tests.Api.GetFid;

[TestClass]
[TestCategory(TestCategoryNames.Integration)]
[TestCategory(TestCategoryNames.ForumFoundation)]
[TestSubject(typeof(GetFidApi))]
public sealed class GetFidTest : TestBase
{
    private const string SafeForumKeyword = "lol欧服";

    [TestMethod]
    public async Task TestRequest()
    {
        var getFid = new GetFidApi(HttpCore);
        var fid = await getFid.RequestAsync(SafeForumKeyword);
        Assert.IsGreaterThan<ulong>(0, fid);
        Console.WriteLine($"{SafeForumKeyword} 的 FID: {fid}");
    }
}
