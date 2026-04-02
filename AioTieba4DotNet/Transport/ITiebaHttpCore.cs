namespace AioTieba4DotNet.Transport;

/// <summary>
///     HTTP 核心接口，负责底层网络请求的打包、签名与发送
/// </summary>
internal interface ITiebaHttpCore
{
    /// <summary>
    ///     当前绑定的账户信息 <see cref="Account" />
    /// </summary>
    Account? Account { get; }

    /// <summary>
    ///     底层 HttpClient 实例 <see cref="HttpClient" />
    /// </summary>
    HttpClient HttpClient { get; }

    /// <summary>
    ///     设置或更新绑定的账户
    /// </summary>
    /// <param name="newAccount">新账户 <see cref="Account" /></param>
    void SetAccount(Account newAccount);

    /// <summary>
    ///     发送自定义 HTTP 请求并返回字符串响应
    /// </summary>
    /// <param name="requestFactory">请求构造委托</param>
    /// <param name="allowRetry">是否允许按策略重试</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应字符串 (string)</returns>
    Task<string> SendAsync(Func<HttpRequestMessage> requestFactory, bool allowRetry = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     发送 App 端表单请求并获取字符串响应
    /// </summary>
    /// <param name="uri">请求 URI</param>
    /// <param name="data">请求表单数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应字符串 (string)</returns>
    Task<string> SendAppFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     发送 App 端 Protobuf 请求并获取字节数组响应
    /// </summary>
    /// <param name="uri">请求 URI</param>
    /// <param name="data">Protobuf 请求负载</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应字节数组 (byte[])</returns>
    Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    ///     发送 Web 端 GET 请求并获取字符串响应
    /// </summary>
    /// <param name="uri">请求 URI</param>
    /// <param name="parameters">查询参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应字符串 (string)</returns>
    Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     发送 Web 端表单请求并获取字符串响应
    /// </summary>
    /// <param name="uri">请求 URI</param>
    /// <param name="data">请求表单数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>响应字符串 (string)</returns>
    Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
        CancellationToken cancellationToken = default);
}
