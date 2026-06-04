using AioTieba4DotNet.Models.Users;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class VirtualImagePfMapper
{
    internal static VirtualImagePf FromTbData(ThreadInfo dataProto)

    {
        var enabled = !string.IsNullOrEmpty(dataProto.CustomFigure?.BackgroundValue);

        var customStateContent = dataProto.CustomState?.Content ?? "";

        return new VirtualImagePf { Enabled = enabled, State = customStateContent };
    }


    internal static VirtualImagePf FromTbData(User.Types.VirtualImageInfo? dataProto)

    {
        return new VirtualImagePf
        {
            Enabled = dataProto?.IssetVirtualImage == 1, State = dataProto?.PersonalState?.Text ?? ""
        };
    }
}
