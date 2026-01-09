namespace AioTieba4DotNet.Core;

public static class Const
{
    public const string MainVersion = "12.72.1.0";
    public const string PostVersion = "12.35.1.0";
    public const string AppSecureScheme = "https";
    public const string AppInsecureScheme = "http";
    public const string AppBaseHost = "tiebac.baidu.com";
    public const string WebBaseHost = "tieba.baidu.com";
    public const string Version = "4.4.9";
    public const int TbcAndroidIdSize = 16;
    public const int TbcMd5HashSize = 16;
    public const int TbcMd5StrSize = (TbcMd5HashSize * 2);
    public const int TbcSha1HashSize = 20;
    public const int TbcSha1HexSize =  (TbcSha1HashSize * 2);
    public static int TbcSha1Base32Size = BASE32_LEN(TbcSha1HashSize);
    public const string SofireHost = "sofire.baidu.com";
    public static int BASE32_LEN(int len)
    {
        return (len / 5) * 8 + ((len % 5 != 0) ? 8 : 0);
    }
}