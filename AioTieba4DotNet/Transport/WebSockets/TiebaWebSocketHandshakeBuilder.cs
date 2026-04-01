using System.Security.Cryptography;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;
using Google.Protobuf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Transport.WebSockets;

internal sealed class TiebaWebSocketHandshakeBuilder
{
    private const string RsaPublicKey =
        "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAwQpwBZxXJV/JVRF/uNfyMSdu7YWwRNLM8+2xbniGp2iIQHOikPpTYQjlQgMi1uvq1kZpJ32rHo3hkwjy2l0lFwr3u4Hk2Wk7vnsqYQjAlYlK0TCzjpmiI+OiPOUNVtbWHQiLiVqFtzvpvi4AU7C1iKGvc/4IS45WjHxeScHhnZZ7njS4S1UgNP/GflRIbzgbBhyZ9kEW5/OO5YfG1fy6r4KSlDJw4o/mw5XhftyIpL+5ZBVBC6E1EIiP/dd9AbK62VV1PByfPMHMixpxI3GM2qwcmFsXcCcgvUXJBa9k6zP8dDQ3csCM2QNT+CQAOxthjtp/TFWaD7MzOdsIYb3THwIDAQAB";

    internal byte[] Pack(Account account)
    {
        var device = new JObject
        {
            ["cuid"] = account.Cuid,
            ["_client_version"] = Const.MainVersion,
            ["_msg_status"] = "1",
            ["cuid_galaxy2"] = account.CuidGalaxy2,
            ["_client_type"] = "2",
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
        };

        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(RsaPublicKey), out _);
        var secretKey = rsa.Encrypt(account.AesEcbSecKey, RSAEncryptionPadding.Pkcs1);

        using var ms = new MemoryStream();
        using var cos = new CodedOutputStream(ms);

        using var innerMs = new MemoryStream();
        using var innerCos = new CodedOutputStream(innerMs);
        innerCos.WriteRawTag(0x0A);
        innerCos.WriteString(account.Bduss);
        innerCos.WriteRawTag(0x12);
        innerCos.WriteString(device.ToString(Formatting.None));
        innerCos.WriteRawTag(0x1A);
        innerCos.WriteBytes(ByteString.CopyFrom(secretKey));
        innerCos.WriteRawTag(0x62);
        innerCos.WriteString(account.Stoken);
        innerCos.Flush();
        var innerData = innerMs.ToArray();

        cos.WriteRawTag(0x0A);
        cos.WriteString($"{account.Cuid}|com.baidu.tieba{Const.MainVersion}");
        cos.WriteRawTag(0x12);
        cos.WriteBytes(ByteString.CopyFrom(innerData));
        cos.Flush();

        return ms.ToArray();
    }
}
