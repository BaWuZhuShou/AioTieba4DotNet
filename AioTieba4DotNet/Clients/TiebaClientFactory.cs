using System.Net.Http;
using AioTieba4DotNet.Contracts;

namespace AioTieba4DotNet;

/// <summary>
///     默认贴吧客户端工厂实现
/// </summary>
public sealed class TiebaClientFactory : ITiebaClientFactory
{
    private readonly TiebaClientComposition _composition;

    /// <summary>
    ///     使用 <see cref="IHttpClientFactory"/> 创建可复用的客户端工厂。
    /// </summary>
    /// <param name="httpClientFactory"><see cref="IHttpClientFactory"/> 实例。</param>
    public TiebaClientFactory(IHttpClientFactory httpClientFactory)
        : this(TiebaClientComposition.CreateForDependencyInjection(httpClientFactory))
    {
    }

    internal TiebaClientFactory(TiebaClientComposition composition)
    {
        _composition = composition;
    }

    /// <inheritdoc/>
    public ITiebaClient CreateClient(TiebaOptions options)
    {
        return _composition.CreateClient(options);
    }

    /// <inheritdoc/>
    public ITiebaClient CreateClient(string bduss, string? stoken = null)
    {
        if (string.IsNullOrWhiteSpace(bduss))
            throw new TiebaConfigurationException("The bduss overload requires a non-empty BDUSS value.");

        return CreateClient(new TiebaOptions { Bduss = bduss, Stoken = stoken });
    }

    /// <inheritdoc/>
    public ITiebaClient CreateClient(AioTieba4DotNet.Contracts.Account account)
    {
        ArgumentNullException.ThrowIfNull(account);
        return CreateClient(account.ToTiebaOptions());
    }
}
