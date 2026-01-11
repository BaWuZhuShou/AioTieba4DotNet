using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DelPostApi = AioTieba4DotNet.Api.DelPost.DelPost;

namespace AioTieba4DotNet.Tests.Api.DelPost;

[TestClass]
[TestSubject(typeof(DelPostApi))]
public class DelPostTest : TestBase
{
    [TestMethod]
    public async Task TestRequestAsync()
    {
        EnsureAuthenticated();

        _ = new DelPostApi(HttpCore);

        // 删帖测试建议在集成测试中进行
        // await delPost.RequestAsync(11626, 8116540605, 123456789);
    }
}
