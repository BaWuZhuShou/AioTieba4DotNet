using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Testing;
using AioTieba4DotNet.Transport.WebSockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests;

[TestClass]
[TestCategory(TestCategoryNames.Integration)]
[TestCategory(TestCategoryNames.MessagingClient)]
public sealed class WebsocketIntegrationTest : TestBase
{
    [TestMethod]
    public async Task TestWsConnectionSuccessAsync()
    {
        await RunWebSocketScenarioOrInconclusiveAsync(async () =>
        {
            // 我们不提供 Account，这样它就不会发送 1001 认证请求，避免因为 BDUSS 错误被踢出
            using var wsCore = new WebsocketCore();
            await wsCore.ConnectAsync();

            // 如果连接成功，我们尝试发送一个心跳
            // 心跳 cmd 为 0，不加密
            await wsCore.SendAsync(0, [], false);
        }, nameof(TestWsConnectionSuccessAsync));
    }

    [TestMethod]
    public async Task TestMultipleConnectionsIsolationAsync()
    {
        await RunWebSocketScenarioOrInconclusiveAsync(async () =>
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
        }, nameof(TestMultipleConnectionsIsolationAsync));
    }

    private static async Task RunWebSocketScenarioOrInconclusiveAsync(Func<Task> action, string operationName)
    {
        try
        {
            await action();
        }
        catch (TiebaWebSocketConnectionLostException exception) when (IsRemoteCloseWithoutHandshake(exception))
        {
            Assert.Inconclusive(
                $"Skipping {operationName}: the remote WebSocket endpoint closed the connection without a close handshake in this environment. {exception.InnerException?.Message ?? exception.Message}");
        }
        catch (TiebaWebSocketUnavailableException exception) when (IsEnvironmentWebSocketHandshakeFailure(exception))
        {
            Assert.Inconclusive(
                $"Skipping {operationName}: the remote WebSocket endpoint could not complete a stable connect/handshake sequence in this environment. {exception.InnerException?.Message ?? exception.Message}");
        }
        catch (TiebaTransportException exception) when (IsRemoteCloseWithoutHandshake(exception))
        {
            Assert.Inconclusive(
                $"Skipping {operationName}: the remote WebSocket endpoint closed the connection without a close handshake in this environment. {exception.InnerException?.Message ?? exception.Message}");
        }
        catch (WebSocketException exception) when (IsRemoteCloseWithoutHandshake(exception))
        {
            Assert.Inconclusive(
                $"Skipping {operationName}: the remote WebSocket endpoint closed the connection without a close handshake in this environment. {exception.Message}");
        }
    }

    private static bool IsRemoteCloseWithoutHandshake(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current is WebSocketException websocketException &&
                websocketException.Message.Contains("without completing the close handshake", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsEnvironmentWebSocketHandshakeFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current is WebSocketException websocketException &&
                (websocketException.Message.Contains("Unable to connect to the remote server", StringComparison.OrdinalIgnoreCase) ||
                 websocketException.Message.Contains("without completing the close handshake", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (current.Message.Contains("远程主机强迫关闭了一个现有的连接", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
