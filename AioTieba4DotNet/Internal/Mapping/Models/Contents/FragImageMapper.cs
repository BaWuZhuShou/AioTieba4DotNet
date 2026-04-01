using System.Text.RegularExpressions;
using AioTieba4DotNet.Models.Contents;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class FragImageMapper
{
    private static readonly Regex ImageHashExp = new("/([a-z0-9]{32,})\\.");

    internal static FragImage FromTbData(PbContent dataProto)

    {
        var src = dataProto.CdnSrc;

        var bigSrc = dataProto.BigCdnSrc;

        var originSrc = dataProto.OriginSrc;

        var originSize = dataProto.OriginSize;


        var bSize = dataProto.Bsize.Split(',');

        var showWidth = 0;

        var showHeight = 0;

        if (bSize.Length >= 2)

        {
            _ = int.TryParse(bSize[0], out showWidth);

            _ = int.TryParse(bSize[1], out showHeight);
        }


        var hash = ImageHashExp.Match(src).Groups[1].Value;


        return new FragImage
        {
            Src = src,
            BigSrc = bigSrc,
            OriginSrc = originSrc,
            OriginSize = originSize,
            ShowWidth = showWidth,
            ShowHeight = showHeight,
            Hash = hash
        };
    }


    internal static FragImage FromTbData(Media dataProto)

    {
        var src = dataProto.SmallPic;

        var bigSrc = dataProto.BigPic;

        var originSrc = dataProto.OriginPic;

        var originSize = dataProto.OriginSize;


        var showWidth = (int)dataProto.Width;

        var showHeight = (int)dataProto.Height;


        var hash = ImageHashExp.Match(src).Groups[1].Value;


        return new FragImage
        {
            Src = src,
            BigSrc = bigSrc,
            OriginSrc = originSrc,
            OriginSize = originSize,
            ShowWidth = showWidth,
            ShowHeight = showHeight,
            Hash = hash
        };
    }
}
