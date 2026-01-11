using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AioTieba4DotNet;

/// <summary>
/// 依赖注入扩展类，用于集成到 Microsoft.Extensions.DependencyInjection
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// 注册 AioTieba4DotNet 相关服务到 DI 容器
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configureOptions">配置参数委托</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddAioTiebaClient(this IServiceCollection services,
        Action<TiebaOptions>? configureOptions = null)
    {
        if (configureOptions != null) services.Configure(configureOptions);

        // 注册专用的 HttpClient，配置连接池、Cookie 容器和 GZip 压缩
        services.AddHttpClient("TiebaClient", client =>
        {
            // 配置默认 Header 等
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new System.Net.CookieContainer(),
            AutomaticDecompression = System.Net.DecompressionMethods.GZip
        });

        // 注册 HTTP 核心组件，并自动注入已注册的拦截器
        services.AddScoped<ITiebaHttpCore>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("TiebaClient");
            var options = sp.GetRequiredService<IOptions<TiebaOptions>>().Value;

            var httpCore = new HttpCore(httpClient);
            if (!string.IsNullOrEmpty(options.Bduss) && !string.IsNullOrEmpty(options.Stoken))
                httpCore.SetAccount(new Account(options.Bduss, options.Stoken));

            return httpCore;
        });

        // 注册各功能模块及主客户端
        services.AddScoped<ITiebaClient, TiebaClient>();
        services.AddScoped<IForumModule, ForumModule>();
        services.AddScoped<IClientModule, ClientModule>();

        services.AddScoped<IThreadModule>(sp =>
        {
            var httpCore = sp.GetRequiredService<ITiebaHttpCore>();
            var forums = sp.GetRequiredService<IForumModule>();
            var wsCore = sp.GetRequiredService<ITiebaWsCore>();
            var options = sp.GetRequiredService<IOptions<TiebaOptions>>().Value;
            return new ThreadModule(httpCore, forums, wsCore) { RequestMode = options.RequestMode };
        });

        services.AddScoped<IUserModule>(sp =>
        {
            var httpCore = sp.GetRequiredService<ITiebaHttpCore>();
            var forums = sp.GetRequiredService<IForumModule>();
            var wsCore = sp.GetRequiredService<ITiebaWsCore>();
            var options = sp.GetRequiredService<IOptions<TiebaOptions>>().Value;
            return new UserModule(httpCore, forums, wsCore) { RequestMode = options.RequestMode };
        });

        services.AddScoped<ITiebaWsCore>(sp =>
        {
            var httpCore = sp.GetRequiredService<ITiebaHttpCore>();
            return new WebsocketCore(httpCore.Account);
        });

        // 注册客户端工厂（单例）
        services.AddSingleton<ITiebaClientFactory, TiebaClientFactory>();

        return services;
    }
}
