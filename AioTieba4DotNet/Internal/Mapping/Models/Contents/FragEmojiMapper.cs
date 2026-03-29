using AioTieba4DotNet.Models.Contents;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class FragEmojiMapper
{
    internal static FragEmoji FromTbData(PbContent dataProto)

        {

            var id = dataProto.Text;

            var desc = dataProto.C;

            return new FragEmoji { Id = id, Desc = desc };

        }
}
