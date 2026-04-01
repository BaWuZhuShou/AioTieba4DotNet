using System;
using System.Threading.Tasks;
using AioTieba4DotNet.Testing;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GetForumApi = AioTieba4DotNet.Api.GetForum.GetForum;
using GetForumDetailApi = AioTieba4DotNet.Api.GetForumDetail.GetForumDetail;

namespace AioTieba4DotNet.Tests.Api.GetForumDetail;

[TestClass]
[TestCategory(TestCategoryNames.Integration)]
[TestCategory(TestCategoryNames.ForumFoundation)]
[TestSubject(typeof(GetForumDetailApi))]
public sealed class GetForumDetailTest : TestBase
{
    private const string SafeForumQuery = "lol欧服吧";
    private const string CanonicalSafeForumName = "lol欧服";

    [TestMethod]
    public async Task TestRequest()
    {
        var getForum = new GetForumApi(HttpCore);
        var getForumDetail = new GetForumDetailApi(HttpCore);
        var forum = await getForum.RequestAsync(SafeForumQuery);
        var result = await getForumDetail.RequestAsync(forum.Fid);
        Assert.IsNotNull(result);
        Assert.AreEqual(CanonicalSafeForumName, result.Fname);
        Console.WriteLine($"Forum Name: {result.Fname}, FID: {result.Fid}");
    }
}
