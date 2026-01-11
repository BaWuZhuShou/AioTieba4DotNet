using System.Reflection;

namespace AioTieba4DotNet.Core;

/// <summary>
///     常量定义
/// </summary>
public static class Const
{
    /// <summary>
    ///     主版本号
    /// </summary>
    public const string MainVersion = "12.64.1.1";

    /// <summary>
    ///     发布版本号
    /// </summary>
    public const string PostVersion = "12.35.1.0";

    /// <summary>
    ///     App 安全协议
    /// </summary>
    public const string AppSecureScheme = "https";

    /// <summary>
    ///     App 非安全协议
    /// </summary>
    public const string AppInsecureScheme = "http";

    /// <summary>
    ///     App 基础主机
    /// </summary>
    public const string AppBaseHost = "tiebac.baidu.com";

    /// <summary>
    ///     Web 基础主机
    /// </summary>
    public const string WebBaseHost = "tieba.baidu.com";

    /// <summary>
    ///     Android ID 长度
    /// </summary>
    public const int TbcAndroidIdSize = 16;

    /// <summary>
    ///     MD5 哈希长度
    /// </summary>
    public const int TbcMd5HashSize = 16;

    /// <summary>
    ///     MD5 字符串长度
    /// </summary>
    public const int TbcMd5StrSize = TbcMd5HashSize * 2;

    /// <summary>
    ///     SHA1 哈希长度
    /// </summary>
    public const int TbcSha1HashSize = 20;

    /// <summary>
    ///     SHA1 十六进制长度
    /// </summary>
    public const int TbcSha1HexSize = TbcSha1HashSize * 2;

    /// <summary>
    ///     Sofire 主机
    /// </summary>
    public const string SofireHost = "sofire.baidu.com";

    /// <summary>
    ///     库版本号
    /// </summary>
    public static readonly string Version =
        typeof(Const).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            .Split('+')[0] ?? "0.0.0";

    /// <summary>
    ///     SHA1 Base32 长度
    /// </summary>
    public static int TbcSha1Base32Size = BASE32_LEN(TbcSha1HashSize);

    /// <summary>
    ///     计算 Base32 编码长度
    /// </summary>
    /// <param name="len">原始长度</param>
    /// <returns>Base32 长度</returns>
    public static int BASE32_LEN(int len)
    {
        return len / 5 * 8 + (len % 5 != 0 ? 8 : 0);
    }
}
