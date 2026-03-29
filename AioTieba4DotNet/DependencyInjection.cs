using System.Net;
using AioTieba4DotNet.Transport.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AioTieba4DotNet;

/// <summary>
///     依赖注入扩展类，用于集成到 Microsoft.Extensions.DependencyInjection
/// </summary>
public static class DependencyInjection
{
    internal const string HttpClientName = "TiebaClient";

    /// <summary>
    ///     注册 AioTieba4DotNet 相关服务到 DI 容器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置参数委托</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddAioTiebaClient(this IServiceCollection services,
        Action<TiebaOptions>? configureOptions = null)
    {
        services.AddOptions<TiebaOptions>();
        if (configureOptions != null) services.Configure(configureOptions);

        // 注册专用的 HttpClient，配置连接池、Cookie 容器和 GZip 压缩
        services.AddHttpClient(HttpClientName, client =>
        {
            TiebaHttpClientFactory.ConfigureNamedClient(client);
        }).ConfigurePrimaryHttpMessageHandler(TiebaHttpClientFactory.CreatePrimaryHandler);

        services.AddScoped<ITiebaClient>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(HttpClientName);
            var options = sp.GetRequiredService<IOptions<TiebaOptions>>().Value;
            return new TiebaClient(TiebaClientComposition.CreateRuntime(options, httpClient));
        });

        services.AddSingleton<ITiebaClientFactory, TiebaClientFactory>();

        return services;
    }
}
