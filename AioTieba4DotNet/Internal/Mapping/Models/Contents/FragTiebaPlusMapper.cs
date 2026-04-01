using AioTieba4DotNet.Models.Contents;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class FragTiebaPlusMapper
{
    internal static FragTiebaPlus FromTbData(PbContent dataProto)

    {
        var text = dataProto.TiebaplusInfo.Desc;

        var url = new Uri(dataProto.TiebaplusInfo.JumpUrl);

        return new FragTiebaPlus { Text = text, Url = url };
    }
}
