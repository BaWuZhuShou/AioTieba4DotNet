using System.Security.Cryptography;
using System.Text;

namespace AioTieba4DotNet.Core;

public class Signer
{
    // 定义签名后缀
    private const string SignSuffix = "tiebaclient!!!";

    // 核心签名方法
    public static string Sign(List<KeyValuePair<string, string>> items)
    {
        // 初始化 MD5 哈希算法
        // 创建一个字符串构建器来逐步添加数据
        var sb = new StringBuilder();
        foreach (var item in items)
        {
            // 获取键并转换为 UTF-8 字符串
            var key = item.Key + "=";
            sb.Append(key);
            sb.Append(item.Value);

        }

        // 添加签名后缀
        sb.Append(SignSuffix);

        // 计算 MD5 哈希值
        var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(sb.ToString()));

        // 将哈希值转换为十六进制字符串
        var hashSb = new StringBuilder();
        foreach (var b in hashBytes)
        {
            hashSb.Append(b.ToString("x2"));
        }
        // 返回最终的 MD5 签名
        return hashSb.ToString();
    }
}