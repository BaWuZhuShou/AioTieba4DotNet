namespace AioTieba4DotNet.Api.Entities.Contents;

/// <summary>
///     视频碎片
/// </summary>
public class FragVideo : IFrag
{
    /// <summary>
    ///     视频链接
    /// </summary>
    public string Src { get; init; } = "";

    /// <summary>
    ///     封面链接
    /// </summary>
    public string CoverSrc { get; init; } = "";

    /// <summary>
    ///     视频长度
    /// </summary>
    public uint Duration { get; init; }

    /// <summary>
    ///     视频宽度
    /// </summary>
    public uint Width { get; init; }

    /// <summary>
    ///     视频高度
    /// </summary>
    public uint Height { get; init; }

    /// <summary>
    ///     浏览次数
    /// </summary>
    public int ViewNum { get; init; }

    /// <summary>
    ///     文本内容
    /// </summary>
    public string Text => "";

    /// <summary>
    ///     获取碎片类型
    /// </summary>
    /// <returns>碎片类型名称</returns>
    public string GetFragType()
    {
        return "FragVideo";
    }

    /// <summary>
    ///     转换为字典用于序列化
    /// </summary>
    /// <returns>包含碎片数据的字典</returns>
    public Dictionary<string, object> ToDict()
    {
        return new Dictionary<string, object> { { "type", "5" }, { "link", Src }, { "src", CoverSrc } };
    }

    /// <summary>
    ///     从贴吧原始数据转换
    /// </summary>
    /// <param name="dataProto">Protobuf 视频信息数据</param>
    /// <returns>视频碎片实体</returns>
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

    /// <summary>
    ///     从贴吧原始数据转换
    /// </summary>
    /// <param name="dataProto">Protobuf 碎片数据</param>
    /// <returns>视频碎片实体</returns>
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

    /// <summary>
    ///     视频是否可用
    /// </summary>
    /// <returns>如果视频宽度大于0则返回true</returns>
    public bool IsValid()
    {
        return Width > 0;
    }

    /// <summary>
    ///     格式设置成员
    /// </summary>
    /// <returns>string</returns>
    public override string ToString()
    {
        return
            $"{GetFragType()} {nameof(Src)}: {Src}, {nameof(CoverSrc)}: {CoverSrc}, {nameof(Duration)}: {Duration}, {nameof(Width)}: {Width}, {nameof(Height)}: {Height}, {nameof(ViewNum)}: {ViewNum}";
    }
}
