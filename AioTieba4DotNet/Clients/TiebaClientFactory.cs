using AioTieba4DotNet.Exceptions;

namespace AioTieba4DotNet;

/// <summary>
///     默认贴吧客户端工厂实现
/// </summary>
/// <param name="httpClientFactory"><see cref="IHttpClientFactory"/> 实例</param>
public sealed class TiebaClientFactory(IHttpClientFactory httpClientFactory) : ITiebaClientFactory
{
    /// <inheritdoc/>
    public ITiebaClient CreateClient(TiebaOptions options)
    {
        var httpClient = httpClientFactory.CreateClient(DependencyInjection.HttpClientName);
        return new TiebaClient(TiebaClientComposition.CreateRuntime(options, httpClient));
    }

    /// <inheritdoc/>
    public ITiebaClient CreateClient(string bduss, string? stoken = null)
    {
        if (string.IsNullOrWhiteSpace(bduss))
            throw new TiebaConfigurationException("The bduss overload requires a non-empty BDUSS value.");

        return CreateClient(new TiebaOptions { Bduss = bduss, Stoken = stoken });
    }
}
