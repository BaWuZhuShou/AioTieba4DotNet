using AioTieba4DotNet.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AioTieba4DotNet.Core;

/// <summary>
///     贴吧客户端工厂
/// </summary>
/// <param name="serviceProvider">服务提供程序</param>
public class TiebaClientFactory(IServiceProvider serviceProvider) : ITiebaClientFactory
{
    /// <summary>
    ///     创建贴吧客户端
    /// </summary>
    /// <param name="options">配置选项</param>
    /// <returns>贴吧客户端实例</returns>
    public ITiebaClient CreateClient(TiebaOptions options)
    {
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("TiebaClient");

        var httpCore = new HttpCore(httpClient);
        if (!string.IsNullOrEmpty(options.Bduss))
            httpCore.SetAccount(new Account(options.Bduss, options.Stoken ?? string.Empty));

        return new TiebaClient(httpCore) { RequestMode = options.RequestMode };
    }

    /// <summary>
    ///     创建贴吧客户端
    /// </summary>
    /// <param name="bduss">BDUSS</param>
    /// <param name="stoken">STOKEN</param>
    /// <returns>贴吧客户端实例</returns>
    public ITiebaClient CreateClient(string bduss, string stoken = "")
    {
        return CreateClient(new TiebaOptions { Bduss = bduss, Stoken = stoken });
    }
}
