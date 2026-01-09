using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AioTieba4DotNet;

public static class DependencyInjection
{
    public static IServiceCollection AddAioTiebaClient(this IServiceCollection services, Action<TiebaOptions>? configureOptions = null)
    {
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        services.AddHttpClient("TiebaClient", client =>
        {
            // 配置默认 Header 等
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new System.Net.CookieContainer(),
            AutomaticDecompression = System.Net.DecompressionMethods.GZip
        });

        services.AddScoped<ITiebaHttpCore>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("TiebaClient");
            var options = sp.GetRequiredService<IOptions<TiebaOptions>>().Value;
            
            var httpCore = new HttpCore(httpClient);
            if (!string.IsNullOrEmpty(options.Bduss) && !string.IsNullOrEmpty(options.Stoken))
            {
                httpCore.SetAccount(new Account(options.Bduss, options.Stoken));
            }
            return httpCore;
        });

        services.AddScoped<ITiebaClient, TiebaClient>();
        services.AddScoped<IForumModule, ForumModule>();
        services.AddScoped<IThreadModule>(sp =>
        {
            var httpCore = sp.GetRequiredService<ITiebaHttpCore>();
            var forums = sp.GetRequiredService<IForumModule>();
            var wsCore = sp.GetRequiredService<ITiebaWsCore>();
            var options = sp.GetRequiredService<IOptions<TiebaOptions>>().Value;
            return new ThreadModule(httpCore, forums, wsCore)
            {
                RequestMode = options.RequestMode
            };
        });
        services.AddScoped<IUserModule, UserModule>();
        services.AddScoped<ITiebaWsCore>(sp =>
        {
            var httpCore = sp.GetRequiredService<ITiebaHttpCore>();
            return new WebsocketCore(httpCore.Account);
        });

        return services;
    }
}
