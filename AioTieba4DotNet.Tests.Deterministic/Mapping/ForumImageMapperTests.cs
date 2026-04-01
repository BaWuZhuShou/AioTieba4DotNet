#nullable enable
using System;
using System.Reflection;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Forums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Mapping;

[TestClass]
public sealed class ForumImageMapperTests
{
    [TestMethod]
    public void ToBytes_ReturnsRawBytes_ForSupportedFormats()
    {
        var bytes = ForumImageMapper.ToBytes([1, 2, 3], "image/png");

        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, bytes.Data);
        Assert.IsFalse(bytes.IsEmpty);
    }

    [TestMethod]
    public void ToImage_ParsesPngBmpAndJpegDimensions()
    {
        var png = ForumImageMapper.ToImage(CreatePng(32, 24), "image/png");
        var bmp = ForumImageMapper.ToImage(CreateBmp(40, 30), "image/bmp");
        var jpeg = ForumImageMapper.ToImage(CreateJpeg(48, 36), "image/jpeg");

        Assert.AreEqual(ForumImageFormat.Png, png.Format);
        Assert.AreEqual(32, png.Width);
        Assert.AreEqual(24, png.Height);
        Assert.AreEqual(ForumImageFormat.Bmp, bmp.Format);
        Assert.AreEqual(40, bmp.Width);
        Assert.AreEqual(30, bmp.Height);
        Assert.AreEqual(ForumImageFormat.Jpeg, jpeg.Format);
        Assert.AreEqual(48, jpeg.Width);
        Assert.AreEqual(36, jpeg.Height);
    }

    [TestMethod]
    public void ToImage_ReturnsEmptyImage_WhenDimensionsCannotBeRead()
    {
        var emptyPng = ForumImageMapper.ToImage([0x89, 0x50], "image/png");
        var emptyBmp = ForumImageMapper.ToImage(new byte[10], "image/bmp");
        var emptyJpeg = ForumImageMapper.ToImage(new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 }, "image/jpeg");
        var zeroPng = ForumImageMapper.ToImage(CreatePng(0, 24), "image/png");
        var negativeBmp = ForumImageMapper.ToImage(CreateBmp(40, -30), "image/bmp");
        var zeroBmp = ForumImageMapper.ToImage(CreateBmp(0, 30), "image/bmp");

        Assert.IsTrue(emptyPng.IsEmpty);
        Assert.IsTrue(emptyBmp.IsEmpty);
        Assert.IsTrue(emptyJpeg.IsEmpty);
        Assert.IsTrue(zeroPng.IsEmpty);
        Assert.AreEqual(40, negativeBmp.Width);
        Assert.AreEqual(30, negativeBmp.Height);
        Assert.IsFalse(negativeBmp.IsEmpty);
        Assert.IsTrue(zeroBmp.IsEmpty);
    }

    [TestMethod]
    public void ToBytes_And_ToImage_RejectUnsupportedContentTypes_AndNullBytes()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => ForumImageMapper.ToBytes(null!, "image/png"));
        Assert.ThrowsExactly<ArgumentNullException>(() => ForumImageMapper.ToImage(null!, "image/png"));
        Assert.ThrowsExactly<TiebaProtocolException>(() => ForumImageMapper.ToBytes([], null));
        Assert.ThrowsExactly<TiebaProtocolException>(() => ForumImageMapper.ToBytes([], "image/gif"));
        Assert.ThrowsExactly<TiebaProtocolException>(() => ForumImageMapper.ToImage([], "image/gif"));
    }

    [TestMethod]
    public void ToImage_AcceptsTrimmedJpgAlias_AndReturnsEmptyForMalformedJpegShapes()
    {
        var aliasImage = ForumImageMapper.ToImage(CreateJpeg(12, 8), " IMAGE/JPG ");
        var invalidHeader = ForumImageMapper.ToImage([0x00, 0x11, 0x22, 0x33], "image/jpeg");
        var markerlessPayload = ForumImageMapper.ToImage([0xFF, 0xD8, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10], "image/jpeg");
        var truncatedAfterMarker = ForumImageMapper.ToImage([0xFF, 0xD8, 0, 0, 0, 0, 0, 0, 0, 0, 0xFF], "image/jpeg");
        var invalidSegmentLength = ForumImageMapper.ToImage([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00], "image/jpeg");
        var truncatedSegmentLength = ForumImageMapper.ToImage([0xFF, 0xD8, 0xFF, 0xE0, 0x00], "image/jpeg");
        var truncatedLengthAfterMarker = ForumImageMapper.ToImage([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10], "image/jpeg");
        var shortFrame = ForumImageMapper.ToImage([0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x07, 0x08, 0x00, 0x10, 0x00], "image/jpeg");
        var shortFrameWithExactLength = ForumImageMapper.ToImage([0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x07, 0x08, 0x00, 0x10, 0x00, 0x00], "image/jpeg");
        var shortStartOfFrame = ForumImageMapper.ToImage([0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x11, 0x08], "image/jpeg");
        var zeroSizedFrame = ForumImageMapper.ToImage([0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x08, 0x08, 0x00, 0x00, 0x00, 0x10, 0x00], "image/jpeg");
        var widthZeroFrame = ForumImageMapper.ToImage([0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x08, 0x08, 0x00, 0x10, 0x00, 0x00, 0x00], "image/jpeg");

        Assert.AreEqual(ForumImageFormat.Jpeg, aliasImage.Format);
        Assert.AreEqual(12, aliasImage.Width);
        Assert.AreEqual(8, aliasImage.Height);
        Assert.IsTrue(invalidHeader.IsEmpty);
        Assert.IsTrue(markerlessPayload.IsEmpty);
        Assert.IsTrue(truncatedAfterMarker.IsEmpty);
        Assert.IsTrue(invalidSegmentLength.IsEmpty);
        Assert.IsTrue(truncatedSegmentLength.IsEmpty);
        Assert.IsTrue(truncatedLengthAfterMarker.IsEmpty);
        Assert.IsTrue(shortFrame.IsEmpty);
        Assert.IsTrue(shortFrameWithExactLength.IsEmpty);
        Assert.IsTrue(shortStartOfFrame.IsEmpty);
        Assert.IsTrue(zeroSizedFrame.IsEmpty);
        Assert.IsTrue(widthZeroFrame.IsEmpty);
    }

    [TestMethod]
    public void ToImage_ReturnsEmptyWhenJpegMarkerHasNoRemainingLengthBytes()
    {
        var trailingMarkerOnly = ForumImageMapper.ToImage(
            [0xFF, 0xD8, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0xFF, 0xE0],
            "image/jpeg");

        Assert.IsTrue(trailingMarkerOnly.IsEmpty);
    }

    [TestMethod]
    public void ToImage_ReturnsEmptyWhenJpegSofFrameHeaderIsTruncated()
    {
        var truncatedFrameHeader = ForumImageMapper.ToImage(
            [0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x07, 0x08, 0x00, 0x10, 0x00, 0x00],
            "image/jpeg");

        Assert.IsTrue(truncatedFrameHeader.IsEmpty);
    }

    [TestMethod]
    public void PrivateTryGetSize_ReturnsFalseForUnknownImageFormat()
    {
        var args = new object[] { Array.Empty<byte>(), (ForumImageFormat)999, 123, 456 };
        var method = typeof(ForumImageMapper).GetMethod("TryGetSize", BindingFlags.NonPublic | BindingFlags.Static);

        var result = (bool)method!.Invoke(null, args)!;

        Assert.IsFalse(result);
        Assert.AreEqual(0, args[2]);
        Assert.AreEqual(0, args[3]);
    }

    private static byte[] CreatePng(int width, int height)
    {
        var bytes = new byte[24];
        bytes[0] = 0x89; bytes[1] = 0x50; bytes[2] = 0x4E; bytes[3] = 0x47;
        bytes[4] = 0x0D; bytes[5] = 0x0A; bytes[6] = 0x1A; bytes[7] = 0x0A;
        bytes[8] = 0x00; bytes[9] = 0x00; bytes[10] = 0x00; bytes[11] = 0x0D;
        bytes[12] = 0x49; bytes[13] = 0x48; bytes[14] = 0x44; bytes[15] = 0x52;
        WriteInt32BigEndian(bytes, 16, width);
        WriteInt32BigEndian(bytes, 20, height);
        return bytes;
    }

    private static byte[] CreateBmp(int width, int height)
    {
        var bytes = new byte[26];
        BitConverter.GetBytes(width).CopyTo(bytes, 18);
        BitConverter.GetBytes(height).CopyTo(bytes, 22);
        return bytes;
    }

    private static byte[] CreateJpeg(int width, int height)
    {
        return
        [
            0xFF, 0xD8,
            0xFF, 0xE0,
            0x00, 0x10,
            0x4A, 0x46, 0x49, 0x46, 0x00,
            0x01, 0x01,
            0x00,
            0x00, 0x01,
            0x00, 0x01,
            0x00, 0x00,
            0xFF, 0xC0,
            0x00, 0x11,
            0x08,
            (byte)(height >> 8), (byte)height,
            (byte)(width >> 8), (byte)width,
            0x03,
            0x01, 0x11, 0x00,
            0x02, 0x11, 0x00,
            0x03, 0x11, 0x00,
            0xFF, 0xD9
        ];
    }

    private static void WriteInt32BigEndian(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }
}
