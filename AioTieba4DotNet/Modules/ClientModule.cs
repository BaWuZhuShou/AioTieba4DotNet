using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.InitZId;
using AioTieba4DotNet.Api.Sync;

namespace AioTieba4DotNet.Modules;

public class ClientModule(ITiebaHttpCore httpCore) : IClientModule
{
    public async Task<string> InitZIdAsync()
    {
        var api = new InitZId(httpCore);
        return await api.RequestAsync();
    }

    public async Task<(string ClientId, string SampleId)> SyncAsync()
    {
        var api = new Sync(httpCore);
        return await api.RequestAsync();
    }
}
