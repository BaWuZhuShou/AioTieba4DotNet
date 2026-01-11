using AioTieba4DotNet.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AioTieba4DotNet.Core;

public class TiebaClientFactory(IServiceProvider serviceProvider) : ITiebaClientFactory
{
    public ITiebaClient CreateClient(TiebaOptions options)
    {
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("TiebaClient");

        var httpCore = new HttpCore(httpClient);
        if (!string.IsNullOrEmpty(options.Bduss))
        {
            httpCore.SetAccount(new Account(options.Bduss, options.Stoken ?? string.Empty));
        }

        return new TiebaClient(httpCore) { RequestMode = options.RequestMode };
    }

    public ITiebaClient CreateClient(string bduss, string stoken = "")
    {
        return CreateClient(new TiebaOptions { Bduss = bduss, Stoken = stoken });
    }
}
