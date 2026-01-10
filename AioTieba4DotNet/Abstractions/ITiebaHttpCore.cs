using AioTieba4DotNet.Core;

namespace AioTieba4DotNet.Abstractions;

/// <summary>
/// HTTP 核心接口，负责底层网络请求的打包、签名与发送
/// </summary>
public interface ITiebaHttpCore
{
    /// <summary>
    /// 当前绑定的账户信息
    /// </summary>
    Account? Account { get; }

    /// <summary>
    /// 底层 HttpClient 实例
    /// </summary>
    HttpClient HttpClient { get; }

    /// <summary>
    /// 设置或更新绑定的账户
    /// </summary>
    void SetAccount(Account newAccount);

    /// <summary>
    /// 发送 App 端表单请求（自动添加签名）
    /// </summary>
    Task<HttpResponseMessage> PackAppFormRequestAsync(Uri uri, List<KeyValuePair<string, string>> data);

    /// <summary>
    /// 发送 App 端 Protobuf 请求（自动添加签名）
    /// </summary>
    Task<HttpResponseMessage> PackProtoRequestAsync(Uri uri, byte[] data);

    /// <summary>
    /// 发送 Web 端 GET 请求
    /// </summary>
    Task<HttpResponseMessage> PackWebGetRequestAsync(Uri uri, List<KeyValuePair<string, string>> parameters);

    /// <summary>
    /// 发送 Web 端表单请求
    /// </summary>
    Task<HttpResponseMessage> PackWebFormRequestAsync(Uri uri, List<KeyValuePair<string, string>> data);
}
