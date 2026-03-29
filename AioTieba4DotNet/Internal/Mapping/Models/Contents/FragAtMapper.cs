using AioTieba4DotNet.Models.Contents;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class FragAtMapper
{
    internal static FragAt FromTbData(PbContent dataProto)

        {

            var text = dataProto.Text;

            var userId = dataProto.Uid;

            return new FragAt { Text = text, UserId = userId };

        }
}
