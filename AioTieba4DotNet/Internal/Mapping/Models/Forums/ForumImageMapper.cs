using System.Diagnostics.CodeAnalysis;
using AioTieba4DotNet.Models.Forums;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class ForumImageMapper
{
    internal static ForumImageBytes ToBytes(byte[] data, string? contentType)
    {
        ArgumentNullException.ThrowIfNull(data);
        _ = ParseFormat(contentType);
        return new ForumImageBytes { Data = data };
    }

    internal static ForumImage ToImage(byte[] data, string? contentType)
    {
        ArgumentNullException.ThrowIfNull(data);

        var format = ParseFormat(contentType);
        if (!TryGetSize(data, format, out var width, out var height) || width <= 0 || height <= 0)
            return new ForumImage();

        return new ForumImage { Data = data, Format = format, Width = width, Height = height };
    }

    private static ForumImageFormat ParseFormat(string? contentType)
    {
        var normalized = contentType?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalized.EndsWith("jpeg", StringComparison.Ordinal) ||
            normalized.EndsWith("jpg", StringComparison.Ordinal))
            return ForumImageFormat.Jpeg;

        if (normalized.EndsWith("png", StringComparison.Ordinal))
            return ForumImageFormat.Png;

        if (normalized.EndsWith("bmp", StringComparison.Ordinal))
            return ForumImageFormat.Bmp;

        throw new TiebaProtocolException($"Expected jpeg, png or bmp, got '{contentType ?? string.Empty}'.");
    }

    private static bool TryGetSize(byte[] data, ForumImageFormat format, out int width, out int height)
    {
        return format switch
        {
            ForumImageFormat.Png => TryGetPngSize(data, out width, out height),
            ForumImageFormat.Bmp => TryGetBmpSize(data, out width, out height),
            ForumImageFormat.Jpeg => TryGetJpegSize(data, out width, out height),
            _ => Fail(out width, out height)
        };
    }

    private static bool TryGetPngSize(byte[] data, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (data.Length < 24)
            return false;

        width = ReadInt32BigEndian(data, 16);
        height = ReadInt32BigEndian(data, 20);
        return width > 0 && height > 0;
    }

    private static bool TryGetBmpSize(byte[] data, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (data.Length < 26)
            return false;

        width = BitConverter.ToInt32(data, 18);
        height = Math.Abs(BitConverter.ToInt32(data, 22));
        return width > 0 && height > 0;
    }

    [SuppressMessage("Critical Code Smell", "S3776:Refactor this method to reduce its Cognitive Complexity",
        Justification =
            "JPEG marker parsing is a protocol-format walker where the current branch structure mirrors the binary format and is easier to audit than a more abstract refactor.")]
    private static bool TryGetJpegSize(byte[] data, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (data.Length < 4 || data[0] != 0xFF || data[1] != 0xD8)
            return false;

        var index = 2;
        while (index + 8 < data.Length)
        {
            while (index < data.Length && data[index] != 0xFF)
                index++;

            if (index + 1 >= data.Length)
                return false;

            var marker = data[index + 1];
            index += 2;
            if (marker == 0xD8 || marker == 0xD9)
                continue;

            if (index + 1 >= data.Length)
                return false;

            var length = (data[index] << 8) | data[index + 1];
            if (length < 2 || index + length > data.Length)
                return false;

            if (marker is 0xC0 or 0xC1 or 0xC2 or 0xC3 or 0xC5 or 0xC6 or 0xC7 or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE
                or 0xCF)
            {
                if (index + 7 >= data.Length)
                    return false;

                height = (data[index + 3] << 8) | data[index + 4];
                width = (data[index + 5] << 8) | data[index + 6];
                return width > 0 && height > 0;
            }

            index += length;
        }

        return false;
    }

    private static int ReadInt32BigEndian(byte[] data, int offset)
    {
        return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
    }

    private static bool Fail(out int width, out int height)
    {
        width = 0;
        height = 0;
        return false;
    }
}
