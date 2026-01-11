namespace AioTieba4DotNet.Api.Profile.GetUInfoProfile.Entities;

/// <summary>
///     虚拟形象
/// </summary>
public class VirtualImagePf
{
    /// <summary>
    ///     是否启用
    /// </summary>
    public bool Enabled;

    /// <summary>
    ///     状态
    /// </summary>
    public string State = "";

    /// <summary>
    ///     从贴吧原始数据转换
    /// </summary>
    /// <param name="dataProto">Protobuf 主题帖信息数据</param>
    /// <returns>虚拟形象实体</returns>
    public static VirtualImagePf FromTbData(ThreadInfo dataProto)
    {
        var enabled = dataProto?.CustomFigure?.BackgroundValue != "";
        var customStateContent = dataProto?.CustomState?.Content ?? "";
        return new VirtualImagePf { Enabled = enabled, State = customStateContent };
    }

    /// <summary>
    ///     从贴吧原始数据转换
    /// </summary>
    /// <param name="dataProto">Protobuf 虚拟形象信息数据</param>
    /// <returns>虚拟形象实体</returns>
    public static VirtualImagePf FromTbData(User.Types.VirtualImageInfo? dataProto)
    {
        return new VirtualImagePf
        {
            Enabled = dataProto?.IssetVirtualImage == 1, State = dataProto?.PersonalState?.Text ?? ""
        };
    }

    /// <summary>
    ///     转换为字符串
    /// </summary>
    /// <returns>摘要</returns>
    public override string ToString()
    {
        return $"{nameof(Enabled)}: {Enabled}, {nameof(State)}: {State}";
    }
}
