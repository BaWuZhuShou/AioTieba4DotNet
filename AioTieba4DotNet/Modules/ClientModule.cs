using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.InitZId;
using AioTieba4DotNet.Api.Sync;

namespace AioTieba4DotNet.Modules;

/// <summary>
/// 客户端基础功能模块 (初始化、同步等)
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
public class ClientModule(ITiebaHttpCore httpCore) : IClientModule
{
    /// <summary>
    /// 初始化 ZID (设备标识)
    /// </summary>
    /// <returns>ZID 字符串</returns>
    public async Task<string> InitZIdAsync()
    {
        var api = new InitZId(httpCore);
        return await api.RequestAsync();
    }

    /// <summary>
    /// 同步客户端状态 (获取 ClientId 和 SampleId)
    /// </summary>
    /// <returns>包含 ClientId 和 SampleId 的元组</returns>
    public async Task<(string ClientId, string SampleId)> SyncAsync()
    {
        var api = new Sync(httpCore);
        return await api.RequestAsync();
    }
}
