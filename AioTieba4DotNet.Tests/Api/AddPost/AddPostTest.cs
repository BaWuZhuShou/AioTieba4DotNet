using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AddPostApi = AioTieba4DotNet.Api.AddPost.AddPost;

namespace AioTieba4DotNet.Tests.Api.AddPost;

[TestClass]
[TestSubject(typeof(AddPostApi))]
public class AddPostTest : TestBase
{
    [TestMethod]
    public async Task TestRequestAsync()
    {
        EnsureAuthenticated();

        var addPost = new AddPostApi(HttpCore, WebsocketCore);

        var content = "这是一条来自 AioTieba4DotNet 单元测试的回复。";

        // 使用一个测试贴进行回复
        var success = await addPost.RequestAsync("", 1, 1, content);
        Assert.IsTrue(success);
    }
}
