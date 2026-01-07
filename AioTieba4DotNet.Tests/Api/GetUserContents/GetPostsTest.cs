using System;
using AioTieba4DotNet.Api.GetUserContents;
using AioTieba4DotNet.Core;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.GetUserContents;

[TestClass]
[TestSubject(typeof(GetPosts))]
public class GetPostsTest
{
    [TestMethod]
    public void TestRequest()
    {
        var account =
            new Account("","");
        var httpCore = new HttpCore();
        httpCore.SetAccount(account);
        var getUInfoPanel = new GetPosts(httpCore);
        var result = getUInfoPanel.RequestAsync(0, 100, 20, "8.9.8.5");
        result.Wait();
        Console.WriteLine(result.Result.Count);
        foreach (var userPosts in result.Result)
        {
            Console.WriteLine(userPosts.Count);
            Console.WriteLine(userPosts.Tid);
            Console.WriteLine(userPosts.Fid);
            foreach (var userPost in userPosts)
            {
                Console.WriteLine(userPost);
            }
        }
    }
}