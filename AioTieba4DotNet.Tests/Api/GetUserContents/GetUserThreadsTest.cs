using System;
using System.Threading.Tasks;
using AioTieba4DotNet.Api.GetUserContents;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.GetUserContents;

[TestClass]
[TestSubject(typeof(GetUserThreads))]
public class GetUserThreadsTest : TestBase
{
    [TestMethod]
    public async Task TestRequest()
    {
        var getUserThreads = new GetUserThreads(HttpCore, WebsocketCore);
        var result = await getUserThreads.RequestAsync(1, 1, true);
        
        Assert.IsNotNull(result);
        Console.WriteLine($"User Threads Count: {result.Count}");
        foreach (var thread in result)
        {
            Console.WriteLine($"Thread Title: {thread.Title}");
            Console.WriteLine($"Thread ID: {thread.Tid}");
            Console.WriteLine($"Create Time: {thread.CreateTime}");
            Console.WriteLine($"Text: {thread.Text}");
        }
    }
}
