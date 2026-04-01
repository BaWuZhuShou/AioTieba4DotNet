using System;
using System.Threading.Tasks;
using AioTieba4DotNet.Api.GetUserContents;
using AioTieba4DotNet.Testing;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.GetUserContents;

[TestClass]
[TestCategory(TestCategoryNames.Live)]
[TestCategory(TestCategoryNames.UserSocial)]
[TestSubject(typeof(GetPosts))]
public sealed class GetPostsTest : TestBase
{
    [TestMethod]
    public async Task TestRequest()
    {
        if (!IsAuthenticated)
        {
            Assert.Inconclusive("BDUSS not provided");
            return;
        }

        var getPosts = new GetPosts(HttpCore, WebsocketCore);
        var result = await getPosts.RequestWsAsync(1, 1, 20, "8.9.8.5");

        Assert.IsNotNull(result);
        Console.WriteLine(result.Count);
        foreach (var userPosts in result)
        {
            Console.WriteLine($"User Posts Count: {userPosts.Count}");
            Console.WriteLine($"Thread ID: {userPosts.Tid}");
            Console.WriteLine($"Forum ID: {userPosts.Fid}");
            foreach (var userPost in userPosts) Console.WriteLine(userPost);
        }
    }
}
