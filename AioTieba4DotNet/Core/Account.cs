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

    private string? _androidId;
    private string? _uuid;
    private string? _cuid;
    private string? _cuidGalaxy2;
    private string? _c3Aid;
    private byte[]? _aesEcbSecKey;
    private Aes? _aesEcbCipher;
    private byte[]? _aesCbcSecKey;
    private Aes? _aesCbcCipher;

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
                return _androidId ??= BitConverter.ToString(RandomNumberGenerator.GetBytes(8)).Replace("-", "").ToLower();
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
                return _uuid ??= Guid.NewGuid().ToString();
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
                return _cuid ??= "baidutiebaapp" + Uuid;
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
                return _cuidGalaxy2 ??= TbCrypto.CuidGalaxy2(AndroidId);
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
                return _c3Aid ??= TbCrypto.C3Aid(AndroidId, Uuid);
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
                return _aesEcbSecKey ??= RandomNumberGenerator.GetBytes(31);
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
                aes.Key = new Rfc2898DeriveBytes(AesEcbSecKey, [0xa4, 0x0b, 0xc8, 0x34, 0xd6, 0x95, 0xf3, 0x13], 5,
                    HashAlgorithmName.SHA1).GetBytes(32);

                _aesEcbCipher = aes;

                return _aesEcbCipher;
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
                return _aesCbcSecKey ??= RandomNumberGenerator.GetBytes(16);
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
                return _aesCbcCipher;
            }
        }
    }
}