using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using AioTieba4DotNet.Internal;
using Google.Protobuf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Transport.WebSockets;

internal sealed class TiebaWebSocketHandshakeBuilder
{
    private const string RsaPublicKey =
        "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAwQpwBZxXJV/JVRF/uNfyMSdu7YWwRNLM8+2xbniGp2iIQHOikPpTYQjlQgMi1uvq1kZpJ32rHo3hkwjy2l0lFwr3u4Hk2Wk7vnsqYQjAlYlK0TCzjpmiI+OiPOUNVtbWHQiLiVqFtzvpvi4AU7C1iKGvc/4IS45WjHxeScHhnZZ7njS4S1UgNP/GflRIbzgbBhyZ9kEW5/OO5YfG1fy6r4KSlDJw4o/mw5XhftyIpL+5ZBVBC6E1EIiP/dd9AbK62VV1PByfPMHMixpxI3GM2qwcmFsXcCcgvUXJBa9k6zP8dDQ3csCM2QNT+CQAOxthjtp/TFWaD7MzOdsIYb3THwIDAQAB";

    [SuppressMessage("Minor Code Smell",
        "S2325:Methods and properties that don't access instance data should be static",
        Justification =
            "The handshake builder is kept as a dedicated collaborator in the WebSocket engine composition so handshake packing can evolve behind a single seam without changing callers.")]
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
        var secretKey = EncryptTiebaHandshakeSecret(rsa, account.AesEcbSecKey);

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

    [SuppressMessage("Security", "S5542:Use secure mode and padding scheme.",
        Justification =
            "Tieba websocket handshake encryption is protocol-defined and must stay aligned with the upstream aiotieba implementation and remote server expectations.")]
    private static byte[] EncryptTiebaHandshakeSecret(RSA rsa, byte[] secretKey)
    {
        return rsa.Encrypt(secretKey, RSAEncryptionPadding.Pkcs1);
    }
}
