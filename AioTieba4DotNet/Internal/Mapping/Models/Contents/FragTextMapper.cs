using AioTieba4DotNet.Models.Contents;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class FragTextMapper
{
    internal static FragText FromTbData(PbContent dataProto)

        {

            var text = dataProto.Text;

            return new FragText { Text = text };

        }



    internal static FragText FromTbData(PostInfoList.Types.PostInfoContent.Types.Abstract dataProto)

        {

            var text = dataProto.Text;

            return new FragText { Text = text };

        }
}
