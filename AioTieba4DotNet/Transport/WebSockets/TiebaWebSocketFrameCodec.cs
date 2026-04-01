using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;
using AioTieba4DotNet;

namespace AioTieba4DotNet.Transport.WebSockets;

internal sealed class TiebaWebSocketFrameCodec
{
    internal byte[] Pack(byte[] data, int cmd, int reqId, Account? account, bool encrypt = true)
    {
        byte flag = 0x08;
        var payload = data;
        if (encrypt && account != null)
        {
            flag |= 0x80;
            payload = account.AesEcbCipher.EncryptEcb(data, PaddingMode.PKCS7);
        }

        var result = new byte[9 + payload.Length];
        result[0] = flag;
        BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(1, 4), cmd);
        BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(5, 4), reqId);
        payload.CopyTo(result.AsSpan(9));
        return result;
    }

    internal (byte[] Data, int Cmd, int ReqId) Parse(byte[] frame, Account? account)
    {
        if (frame.Length < 9)
            throw new TiebaProtocolException("WebSocket frame was shorter than the 9-byte Tieba header.");

        var flag = frame[0];
        var cmd = BinaryPrimitives.ReadInt32BigEndian(frame.AsSpan(1, 4));
        var reqId = BinaryPrimitives.ReadInt32BigEndian(frame.AsSpan(5, 4));

        var payload = frame[9..];
        if ((flag & 0x80) != 0 && account != null)
            payload = account.AesEcbCipher.DecryptEcb(payload, PaddingMode.PKCS7);

        if ((flag & 0x40) != 0)
        {
            using var input = new MemoryStream(payload);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            payload = output.ToArray();
        }

        return (payload, cmd, reqId);
    }
}
