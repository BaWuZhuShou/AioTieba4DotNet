using System.Security.Cryptography;

namespace AioTieba4DotNet.Core;

/// <summary>
/// 贴吧账户类，包含账户凭证及设备信息
/// </summary>
/// <param name="bduss">BDUSS 凭证</param>
/// <param name="stoken">STOKEN 凭证（可选）</param>
public class Account(string bduss = "", string stoken = "")
{
    /// <summary>
    /// BDUSS 凭证，主登录凭证
    /// </summary>
    public string Bduss { get; private set; } = bduss;

    /// <summary>
    /// STOKEN 凭证，部分 API 校验所需
    /// </summary>
    public string Stoken { get; private set; } = stoken;

    private volatile string? _androidId;
    private volatile string? _uuid;
    private volatile string? _cuid;
    private volatile string? _cuidGalaxy2;
    private volatile string? _c3Aid;
    private volatile byte[]? _aesEcbSecKey;
    private volatile Aes? _aesEcbCipher;
    private volatile byte[]? _aesCbcSecKey;
    private volatile Aes? _aesCbcCipher;

    private readonly object _lock = new();

    /// <summary>
    /// Android ID，自动生成的 16 位 16 进制字符串
    /// </summary>
    public string AndroidId
    {
        get
        {
            if (_androidId != null) return _androidId;
            lock (_lock)
            {
                if (_androidId != null) return _androidId;
                var val = Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();
                _androidId = val;
                return val;
            }
        }
        set => _androidId = value;
    }

    /// <summary>
    /// UUID，自动生成的标准 GUID
    /// </summary>
    public string Uuid
    {
        get
        {
            if (_uuid != null) return _uuid;
            lock (_lock)
            {
                if (_uuid != null) return _uuid;
                var val = Guid.NewGuid().ToString();
                _uuid = val;
                return val;
            }
        }
        set => _uuid = value;
    }

    /// <summary>
    /// 当前会话的 tbs 校验码
    /// </summary>
    public string? Tbs { get; set; }

    private string? ClientId { get; set; }

    private string? SampleId { get; set; }

    /// <summary>
    /// CUID，由百度生成的设备唯一标识符
    /// </summary>
    public string Cuid
    {
        get
        {
            if (_cuid != null) return _cuid;
            lock (_lock)
            {
                if (_cuid != null) return _cuid;
                var val = "baidutiebaapp" + Uuid;
                _cuid = val;
                return val;
            }
        }
    }

    /// <summary>
    /// CUID Galaxy 2，新型设备 ID 标识
    /// </summary>
    public string CuidGalaxy2
    {
        get
        {
            if (_cuidGalaxy2 != null) return _cuidGalaxy2;
            lock (_lock)
            {
                if (_cuidGalaxy2 != null) return _cuidGalaxy2;
                var val = TbCrypto.CuidGalaxy2(AndroidId);
                _cuidGalaxy2 = val;
                return val;
            }
        }
    }

    private string? C3Aid
    {
        get
        {
            if (_c3Aid != null) return _c3Aid;
            lock (_lock)
            {
                if (_c3Aid != null) return _c3Aid;
                var val = TbCrypto.C3Aid(AndroidId, Uuid);
                _c3Aid = val;
                return val;
            }
        }
    }

    private string? ZId { get; set; }

    /// <summary>
    /// WebSocket 通信使用的 AES-ECB 密钥
    /// </summary>
    public byte[] AesEcbSecKey
    {
        get
        {
            if (_aesEcbSecKey != null) return _aesEcbSecKey;
            lock (_lock)
            {
                if (_aesEcbSecKey != null) return _aesEcbSecKey;
                var val = RandomNumberGenerator.GetBytes(31);
                _aesEcbSecKey = val;
                return val;
            }
        }
    }

    /// <summary>
    /// WebSocket 通信使用的 AES-ECB 加密器
    /// </summary>
    public Aes AesEcbCipher
    {
        get
        {
            if (_aesEcbCipher != null) return _aesEcbCipher;
            lock (_lock)
            {
                if (_aesEcbCipher != null) return _aesEcbCipher;
                var aes = Aes.Create();
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = Rfc2898DeriveBytes.Pbkdf2(AesEcbSecKey, (byte[])[0xa4, 0x0b, 0xc8, 0x34, 0xd6, 0x95, 0xf3, 0x13], 5,
                    HashAlgorithmName.SHA1, 32);

                _aesEcbCipher = aes;

                return aes;
            }
        }
    }

    /// <summary>
    /// HTTP 某些协议使用的 AES-CBC 密钥
    /// </summary>
    public byte[] AesCbcSecKey
    {
        get
        {
            if (_aesCbcSecKey != null) return _aesCbcSecKey;
            lock (_lock)
            {
                if (_aesCbcSecKey != null) return _aesCbcSecKey;
                var val = RandomNumberGenerator.GetBytes(16);
                _aesCbcSecKey = val;
                return val;
            }
        }
        set => _aesCbcSecKey = value;
    }

    /// <summary>
    /// HTTP 某些协议使用的 AES-CBC 加密器
    /// </summary>
    public Aes? AesCbcCipher
    {
        get
        {
            if (_aesCbcCipher != null) return _aesCbcCipher;
            lock (_lock)
            {
                if (_aesCbcCipher != null) return _aesCbcCipher;
                var aes = Aes.Create();
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;
                aes.Key = AesCbcSecKey;
                aes.IV = new byte[16];
                _aesCbcCipher = aes;
                return aes;
            }
        }
    }
}
