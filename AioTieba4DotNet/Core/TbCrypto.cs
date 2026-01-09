using System.IO.Hashing;
using System.Security.Cryptography;
using System.Text;

namespace AioTieba4DotNet.Core;

/// <summary>
/// 导入所需哈希类
/// 导入加密类
/// </summary>
public static class TbCrypto
{
    private const int HasherNum = 4;
    private const int StepSize = 5;
    private const int HashSizeInBit = 32;

    private static readonly byte[] Cuid2Prefix = "com.baidu"u8.ToArray();
    private static readonly byte[] Cuid3Prefix = "com.helios"u8.ToArray();
    private static readonly char[] HexUppercaseTable = "0123456789ABCDEF".ToCharArray();

    // 更新哈希的内部状态
    private static ulong TbcUpdate(ulong sec, ulong hashVal, ulong start, bool flag)
    {
        var end = start + HashSizeInBit;
        var secTemp = sec;
        var var9 = ((ulong)1 << (int)end) - 1;
        var var5 = (var9 & sec) >> (int)start;

        if (flag)
        {
            var5 ^= hashVal;
        }
        else
        {
            var5 &= hashVal;
        }

        for (ulong i = 0; i < HashSizeInBit; i++)
        {
            var opIdx = start + i;
            if ((var5 & ((ulong)1 << (int)i)) != 0)
            {
                secTemp |= (ulong)1 << (int)opIdx;
            }
            else
            {
                secTemp &= ~((ulong)1 << (int)opIdx);
            }
        }

        return secTemp;
    }


    // 将内部状态写入缓冲区
    private static byte[] TbcWriteBuffer(ulong sec)
    {
        var buffer = new byte[StepSize];
        var tmpSec = sec;
        for (var i = 0; i < StepSize; i++)
        {
            buffer[i] = (byte)(tmpSec & 0xFF);
            tmpSec >>= 8;
        }

        return buffer;
    }

    // 实现Helios哈希，返回结果
    private static byte[] TbcHeliosHash(ReadOnlySpan<byte> src, int size)
    {
        // 初始化 sec 和缓冲区
        var sec = ((ulong)1 << 40) - 1; // 等同于 Java 中的 -1L>>>-40L
        Span<byte> buffer = stackalloc byte[HasherNum * StepSize]; // 使用 stackalloc 分配栈内存
        buffer[..StepSize].Fill(0xFF); // 初始化为 -1（即全 1）

        // 使用 CRC32 进行第一次哈希
        var crc32 = new Crc32();
        crc32.Append(src[..size]);
        crc32.Append(buffer[..StepSize]);
        var crc32Val = crc32.GetCurrentHashAsUInt32();
        sec = TbcUpdate(sec, crc32Val, 8, false);

        // 将结果写入缓冲区
        TbcWriteBuffer(sec).CopyTo(buffer.Slice(StepSize, StepSize));

        // 使用 XXHash32 进行第二次哈希
        var xxhash = new XxHash32();
        xxhash.Append(src[..size]);
        xxhash.Append(buffer[..(StepSize * 2)]);
        var currentHashAsUInt32 = xxhash.GetCurrentHashAsUInt32();

        sec = TbcUpdate(sec, currentHashAsUInt32, 0, true);
        TbcWriteBuffer(sec).CopyTo(buffer.Slice(StepSize * 2, StepSize));

        // 再次使用 XXHash32 进行第三次哈希
        xxhash.Append(buffer.Slice(StepSize * 2, StepSize));
        currentHashAsUInt32 = xxhash.GetCurrentHashAsUInt32();

        sec = TbcUpdate(sec, currentHashAsUInt32, 1, true);
        TbcWriteBuffer(sec).CopyTo(buffer.Slice(StepSize * 3, StepSize));

        // 使用 CRC32 进行第四次哈希
        crc32.Append(buffer.Slice(StepSize, StepSize * 3));
        crc32Val = crc32.GetCurrentHashAsUInt32();
        sec = TbcUpdate(sec, crc32Val, 7, true);

        // 返回最终结果
        return TbcWriteBuffer(sec).ToArray();
    }


    // 生成 CUID Galaxy 2 并返回结果
    private static string TbcCuidGalaxy2(byte[] androidId)
    {
        // 构建MD5输入缓冲区
        var md5Buffer = new byte[Cuid2Prefix.Length + Const.TbcAndroidIdSize];
        Buffer.BlockCopy(Cuid2Prefix, 0, md5Buffer, 0, Cuid2Prefix.Length);
        Buffer.BlockCopy(androidId, 0, md5Buffer, Cuid2Prefix.Length, Const.TbcAndroidIdSize);

        // 计算MD5
        var md5Hash = MD5.HashData(md5Buffer);
        var sb = new StringBuilder();
        foreach (var b in md5Hash)
        {
            sb.Append(HexUppercaseTable[b >> 4]);
            sb.Append(HexUppercaseTable[b & 0x0F]);
        }

        // 添加连接符
        sb.Append("|V");

        // 计算Helios哈希
        var heliosHash = TbcHeliosHash(Encoding.UTF8.GetBytes(sb.ToString()), Const.TbcMd5StrSize);

        // 使用Base32编码
        sb.Append(Base32Encode(heliosHash));

        return sb.ToString();
    }

    // 生成 CUID 3 AID 并返回结果
    private static string? TbcC3Aid(ReadOnlySpan<byte> androidId, ReadOnlySpan<byte> uuid)
    {
        var sha1InputLength = Cuid3Prefix.Length + Const.TbcAndroidIdSize + uuid.Length;
        var dstOffset = 5 + Const.TbcSha1Base32Size;

        // 使用 stackalloc 在栈上分配内存，避免堆分配
        Span<byte> sha1Buffer = stackalloc byte[sha1InputLength];

        // 构建SHA1输入缓冲区
        Cuid3Prefix.CopyTo(sha1Buffer);
        androidId.CopyTo(sha1Buffer.Slice(Cuid3Prefix.Length, Const.TbcAndroidIdSize));
        uuid.CopyTo(sha1Buffer.Slice(Cuid3Prefix.Length + Const.TbcAndroidIdSize, uuid.Length));

        // 计算SHA1
        Span<byte> sha1Hash = stackalloc byte[20]; // SHA1 的输出大小是固定的 20 字节
        SHA1.HashData(sha1Buffer, sha1Hash);

        // 使用 StringBuilderCache 减少分配
        var sb = new StringBuilder(50); // 预估容量，减少动态扩展
        sb.Append("A00-");
        sb.Append(Base32Encode(sha1Hash));
        sb.Append('-');

        // 计算Helios哈希
        Span<byte> sbBytes = stackalloc byte[sb.Length];
        Encoding.UTF8.GetBytes(sb.ToString(), sbBytes);
        var heliosHash = TbcHeliosHash(sbBytes, dstOffset);

        // 使用Base32编码
        sb.Append(Base32Encode(heliosHash));

        return sb.ToString();
    }

    // Base32编码实现，返回编码后的字符串
    private static string Base32Encode(Span<byte> input)
    {
        const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var sb = new StringBuilder();
        var bitBuffer = 0;
        var bitCount = 0;

        foreach (var t in input)
        {
            bitBuffer = (bitBuffer << 8) | t;
            bitCount += 8;

            while (bitCount >= 5)
            {
                var index = (bitBuffer >> (bitCount - 5)) & 31;
                sb.Append(base32Chars[index]);
                bitCount -= 5;
            }
        }

        if (bitCount <= 0) return sb.ToString();
        {
            var index = (bitBuffer << (5 - bitCount)) & 31;
            sb.Append(base32Chars[index]);
        }

        return sb.ToString();
    }

    public static string CuidGalaxy2(string androidId)
    {
        var androidIdBytes = Encoding.UTF8.GetBytes(androidId);
        if (androidIdBytes.Length != 16)
        {
            throw new ArgumentException($"Invalid size of android_id. Expected 16, got {androidIdBytes.Length}");
        }

        return TbcCuidGalaxy2(androidIdBytes);
    }

    public static string? C3Aid(string androidId, string uuid)
    {
        var androidIdBytes = Encoding.UTF8.GetBytes(androidId);
        var uuidBytes = Encoding.UTF8.GetBytes(uuid);
        if (androidIdBytes.Length != 16)
        {
            throw new ArgumentException($"Invalid size of android_id. Expected 16, got {androidIdBytes.Length}");
        }

        if (uuidBytes.Length != 36)
        {
            throw new ArgumentException($"Invalid size of uuid. Expected 36, got {uuidBytes.Length}");
        }

        return TbcC3Aid(androidIdBytes, uuidBytes);
    }
}