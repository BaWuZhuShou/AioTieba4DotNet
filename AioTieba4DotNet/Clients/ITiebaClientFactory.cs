namespace AioTieba4DotNet;

/// <summary>
///     贴吧客户端工厂
/// </summary>
public interface ITiebaClientFactory
{
    /// <summary>
    ///     按配置创建客户端
    /// </summary>
    /// <param name="options">客户端配置</param>
    /// <returns><see cref="ITiebaClient" /> 实例</returns>
    ITiebaClient CreateClient(TiebaOptions options);

    /// <summary>
    ///     按凭据创建客户端
    /// </summary>
    /// <param name="bduss">用户 BDUSS</param>
    /// <param name="stoken">用户 STOKEN</param>
    /// <returns><see cref="ITiebaClient" /> 实例</returns>
    ITiebaClient CreateClient(string bduss, string? stoken = null);

    /// <summary>
    ///     按公开账户对象创建客户端
    /// </summary>
    /// <param name="account">账户凭据对象</param>
    /// <returns><see cref="ITiebaClient" /> 实例</returns>
    ITiebaClient CreateClient(Contracts.Account account);
}
