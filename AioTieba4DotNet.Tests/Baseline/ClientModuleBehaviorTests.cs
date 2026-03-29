#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Modules;
using AioTieba4DotNet.Protocols;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Baseline;

[TestClass]
public class ClientModuleBehaviorTests
{
    [TestMethod]
    public async Task ClientModule_DelegatesInitWebSocketToInternalProtocol()
    {
        var protocol = new RecordingClientProtocol();
        var module = new ClientModule(protocol);

        await module.InitWebSocketAsync();

        Assert.AreEqual(1, protocol.InitWebSocketCalls);
    }

    [TestMethod]
    public async Task ClientModule_DelegatesInitZIdToInternalProtocol()
    {
        var protocol = new RecordingClientProtocol();
        var module = new ClientModule(protocol);

        var result = await module.InitZIdAsync();

        Assert.AreEqual("zid-123", result);
        Assert.AreEqual(1, protocol.InitZIdCalls);
    }

    [TestMethod]
    public async Task ClientModule_DelegatesSyncToInternalProtocol()
    {
        var protocol = new RecordingClientProtocol();
        var module = new ClientModule(protocol);

        var result = await module.SyncAsync();

        Assert.AreEqual(("client-1", "sample-1"), result);
        Assert.AreEqual(1, protocol.SyncCalls);
    }

    private sealed class RecordingClientProtocol : IClientProtocol
    {
        public int InitWebSocketCalls { get; private set; }

        public int InitZIdCalls { get; private set; }

        public int SyncCalls { get; private set; }

        public Task InitWebSocketAsync(CancellationToken cancellationToken = default)
        {
            InitWebSocketCalls++;
            return Task.CompletedTask;
        }

        public Task<string> InitZIdAsync(CancellationToken cancellationToken = default)
        {
            InitZIdCalls++;
            return Task.FromResult("zid-123");
        }

        public Task<(string ClientId, string SampleId)> SyncAsync(CancellationToken cancellationToken = default)
        {
            SyncCalls++;
            return Task.FromResult(("client-1", "sample-1"));
        }
    }
}
