using System.Security.Cryptography;

namespace AioTieba4DotNet.Core;

public static class Utils
{
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

    public static byte[] ApplyPkcs7Padding(byte[] data, int blockSize)
    {
        var paddingSize = blockSize - (data.Length % blockSize);
        var paddedData = new byte[data.Length + paddingSize];
        Buffer.BlockCopy(data, 0, paddedData, 0, data.Length);

        for (var i = data.Length; i < paddedData.Length; i++)
        {
            paddedData[i] = (byte)paddingSize;
        }

        return paddedData;
    }

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
    /// 判断是否是portrait
    /// </summary>
    /// <param name="portrait"></param>
    /// <returns></returns>
    public static bool IsPortrait(string portrait)
    {
        return portrait.Contains("tb.");
    }

    /// <summary>
    /// 转换贴吧数字
    /// </summary>
    /// <param name="tbNum"></param>
    /// <returns></returns>
    public static int TbNumToInt(string tbNum)
    {
        if (!string.IsNullOrEmpty(tbNum) && tbNum.EndsWith('万'))
        {
            // 去掉字符串末尾的"万"，转换为浮点数后乘以10000
            return (int)(double.Parse(tbNum.TrimEnd('万')) * 1e4);
        }
        else
        {
            return int.Parse(tbNum);
        }
    }
}