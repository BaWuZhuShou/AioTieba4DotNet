namespace AioTieba4DotNet.Core;

/// <summary>
/// 实现 RC4（Rivest Cipher 4）加密算法的类，并额外进行一次 XOR(42) 处理。
/// </summary>
public class Rc4
{
    private readonly byte[] _s = new byte[256];
    private int _x;
    private int _y;
    private const byte XorConst = 42; // 定义常量 42

    /// <summary>
    /// 使用给定的密钥初始化 RC4 算法。
    /// </summary>
    /// <param name="key">用于加密/解密的密钥。</param>
    /// <exception cref="ArgumentNullException">当 key 为 null 时抛出。</exception>
    /// <exception cref="ArgumentException">当 key 为空时抛出。</exception>
    public Rc4(byte[] key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key), "密钥不能为空。");
        if (key.Length == 0)
            throw new ArgumentException("密钥长度必须大于零。", nameof(key));
        KeySetup(key);
    }

    /// <summary>
    /// 进行加密或解密。
    /// RC4 是对称流加密算法，因此相同的操作可用于加密和解密。
    /// </summary>
    /// <param name="data">要加密或解密的数据。</param>
    /// <returns>加密或解密后的字节数组。</returns>
    /// <exception cref="ArgumentNullException">当 data 为 null 时抛出。</exception>
    public byte[] Crypt(byte[] data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data), "数据不能为空。");
        var output = new byte[data.Length];
        for (var i = 0; i < data.Length; i++)
        {
            _x = (_x + 1) & 255;
            _y = (_y + _s[_x]) & 255;
            Swap(_x, _y);

            var xorIndex = (byte)((_s[_x] + _s[_y]) & 255);
            // 先进行RC4加密/解密，然后额外进行一次42异或
            output[i] = (byte)((data[i] ^ _s[xorIndex]) ^ XorConst);
        }

        return output;
    }

    /// <summary>
    /// 初始化 RC4 密钥调度算法（KSA）。
    /// 该方法使用给定的密钥对状态数组 _s 进行初始化。
    /// </summary>
    /// <param name="key">用于初始化的密钥。</param>
    private void KeySetup(byte[] key)
    {
        var keyLength = key.Length;
        for (var i = 0; i < 256; i++)
        {
            _s[i] = (byte)i;
        }

        var j = 0;
        for (var i = 0; i < 256; i++)
        {
            j = (j + _s[i] + key[i % keyLength]) & 255;
            Swap(i, j);
        }
    }

    /// <summary>
    /// 交换状态数组 _s 中索引 i 和 j 处的值。
    /// </summary>
    /// <param name="i">第一个索引。</param>
    /// <param name="j">第二个索引。</param>
    private void Swap(int i, int j)
    {
        (_s[i], _s[j]) = (_s[j], _s[i]);
    }
}