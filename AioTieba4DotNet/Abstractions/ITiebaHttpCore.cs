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
    /// 发送 App 端表单请求并获取字符串响应
    /// </summary>
    Task<string> SendAppFormAsync(Uri uri, List<KeyValuePair<string, string>> data);

    /// <summary>
    /// 发送 App 端 Protobuf 请求并获取字节数组响应
    /// </summary>
    Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data);

    /// <summary>
    /// 发送 Web 端 GET 请求并获取字符串响应
    /// </summary>
    Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters);

    /// <summary>
    /// 发送 Web 端表单请求并获取字符串响应
    /// </summary>
    Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data);
}
