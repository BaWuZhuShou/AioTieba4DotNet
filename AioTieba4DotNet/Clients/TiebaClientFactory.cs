using AioTieba4DotNet.Exceptions;

namespace AioTieba4DotNet;

public sealed class TiebaClientFactory(IHttpClientFactory httpClientFactory) : ITiebaClientFactory
{
    public ITiebaClient CreateClient(TiebaOptions options)
    {
        var httpClient = httpClientFactory.CreateClient(DependencyInjection.HttpClientName);
        return new TiebaClient(TiebaClientComposition.CreateRuntime(options, httpClient));
    }

    public ITiebaClient CreateClient(string bduss, string? stoken = null)
    {
        if (string.IsNullOrWhiteSpace(bduss))
            throw new TiebaConfigurationException("The bduss overload requires a non-empty BDUSS value.");

        return CreateClient(new TiebaOptions { Bduss = bduss, Stoken = stoken });
    }
}
