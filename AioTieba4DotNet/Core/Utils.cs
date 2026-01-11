using System.Security.Cryptography;

namespace AioTieba4DotNet.Core;

/// <summary>
///     工具类
/// </summary>
public static class Utils
{
    /// <summary>
    ///     生成随机 Android ID
    /// </summary>
    /// <returns>Android ID 字符串</returns>
    public static string GenerateAndroidId()
    {
        // 创建一个长度为8的字节数组
        var randomBytes = new byte[8];

        // 使用随机数生成器填充字节数组
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        // 将字节数组转换为十六进制字符串
        var hexString = BitConverter.ToString(randomBytes).Replace("-", "").ToLower();

        return hexString;
    }

    /// <summary>
    ///     应用 PKCS7 填充
    /// </summary>
    /// <param name="data">原始数据</param>
    /// <param name="blockSize">块大小</param>
    /// <returns>填充后的数据</returns>
    public static byte[] ApplyPkcs7Padding(byte[] data, int blockSize)
    {
        var paddingSize = blockSize - data.Length % blockSize;
        var paddedData = new byte[data.Length + paddingSize];
        Buffer.BlockCopy(data, 0, paddedData, 0, data.Length);

        for (var i = data.Length; i < paddedData.Length; i++) paddedData[i] = (byte)paddingSize;

        return paddedData;
    }

    /// <summary>
    ///     移除 PKCS7 填充
    /// </summary>
    /// <param name="paddedData">填充后的数据</param>
    /// <param name="blockSize">块大小</param>
    /// <returns>原始数据</returns>
    /// <exception cref="ArgumentException"></exception>
    public static byte[] RemovePkcs7Padding(byte[] paddedData, int blockSize)
    {
        if (paddedData.Length == 0)
            throw new ArgumentException("The data cannot be empty", nameof(paddedData));

        // The last byte indicates the padding size
        var paddingSize = paddedData[paddedData.Length - 1];

        // Validate padding size
        if (paddingSize < 1 || paddingSize > blockSize)
            throw new ArgumentException("Invalid padding size", nameof(paddedData));

        // Ensure the padding size does not exceed the block size
        if (paddedData.Length < paddingSize)
            throw new ArgumentException("Padding size is greater than the length of the data", nameof(paddedData));

        // Create a new array with the data without padding
        var dataLength = paddedData.Length - paddingSize;
        var data = new byte[dataLength];
        Buffer.BlockCopy(paddedData, 0, data, 0, dataLength);

        return data;
    }

    /// <summary>
    ///     判断是否是 portrait
    /// </summary>
    /// <param name="portrait">头像 ID</param>
    /// <returns>True 如果是 portrait</returns>
    public static bool IsPortrait(string portrait)
    {
        return portrait.Contains("tb.");
    }

    /// <summary>
    ///     转换贴吧热度数字（处理“万”单位）
    /// </summary>
    /// <param name="tbNum">贴吧热度字符串</param>
    /// <returns>整数热度值</returns>
    public static int TbNumToInt(string tbNum)
    {
        if (!string.IsNullOrEmpty(tbNum) && tbNum.EndsWith('万'))
            // 去掉字符串末尾的"万"，转换为浮点数后乘以10000
            return (int)(double.Parse(tbNum.TrimEnd('万')) * 1e4);
        return int.Parse(tbNum);
    }
}
