namespace AioTieba4DotNet.Api.Profile.GetUInfoProfile.Entities;

public class VirtualImagePf
{
    public bool Enabled;
    public string State = "";

    public static VirtualImagePf FromTbData(ThreadInfo dataProto)
    {
        var enabled = dataProto?.CustomFigure?.BackgroundValue != "";
        var customStateContent = dataProto?.CustomState?.Content ?? "";
        return new VirtualImagePf { Enabled = enabled, State = customStateContent };
    }

    public static VirtualImagePf FromTbData(User.Types.VirtualImageInfo? dataProto)
    {
        return new VirtualImagePf
        {
            Enabled = dataProto?.IssetVirtualImage == 1, State = dataProto?.PersonalState?.Text ?? ""
        };
    }

    public override string ToString()
    {
        return $"{nameof(Enabled)}: {Enabled}, {nameof(State)}: {State}";
    }
}
