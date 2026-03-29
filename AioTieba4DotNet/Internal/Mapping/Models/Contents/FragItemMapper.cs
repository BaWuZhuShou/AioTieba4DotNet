using AioTieba4DotNet.Models.Contents;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class FragItemMapper
{
    internal static FragItem FromTbData(PbContent dataProto)

        {

            var text = dataProto.Item.ItemName;

            return new FragItem { Text = text };

        }
}
