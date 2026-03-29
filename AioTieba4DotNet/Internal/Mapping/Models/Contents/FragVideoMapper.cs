using AioTieba4DotNet.Models.Contents;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class FragVideoMapper
{
    internal static FragVideo FromTbData(VideoInfo dataProto)

        {

            var src = dataProto.VideoUrl;

            var coverSrc = dataProto.ThumbnailUrl;

            var duration = dataProto.VideoDuration;

            var width = dataProto.VideoWidth;

            var height = dataProto.VideoHeight;

            var viewNum = dataProto.PlayCount;

            return new FragVideo

            {

                Src = src,

                CoverSrc = coverSrc,

                Duration = duration,

                Width = width,

                Height = height,

                ViewNum = viewNum

            };

        }



    internal static FragVideo FromTbData(PbContent dataProto)

        {

            return new FragVideo

            {

                Src = dataProto.Link,

                CoverSrc = dataProto.Src,

                Duration = dataProto.DuringTime,

                Width = dataProto.Width,

                Height = dataProto.Height,

                ViewNum = dataProto.Count

            };

        }
}
