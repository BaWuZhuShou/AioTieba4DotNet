using System.Threading.Tasks;
using AioTieba4DotNet.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests;

[TestClass]
public class WebsocketIntegrationTest
{
    [TestMethod]
    [Ignore("Requires active internet connection and valid tieba endpoint")]
    public async Task TestWsConnectionSuccessAsync()
    {
        // 我们不提供 Account，这样它就不会发送 1001 认证请求，避免因为 BDUSS 错误被踢出
        using var wsCore = new WebsocketCore();
        await wsCore.ConnectAsync();

        // 如果连接成功，我们尝试发送一个心跳
        // 心跳 cmd 为 0，不加密
        await wsCore.SendAsync(0, [], false);

        // 验证状态
        // 虽然我们无法直接访问 _ws，但如果没有抛出异常，说明 Send 成功了
    }

    [TestMethod]
    [Ignore("Requires active internet connection")]
    public async Task TestMultipleConnectionsIsolationAsync()
    {
        using var wsCore1 = new WebsocketCore();
        using var wsCore2 = new WebsocketCore();

        await wsCore1.ConnectAsync();
        await wsCore2.ConnectAsync();

        // 验证它们是否都能正常工作
        await wsCore1.SendAsync(0, [], false);
        await wsCore2.SendAsync(0, [], false);

        // 如果它们共享连接，其中一个 Close 后另一个也会失效
        wsCore1.Dispose();

        await wsCore2.SendAsync(0, [], false);
    }
}
