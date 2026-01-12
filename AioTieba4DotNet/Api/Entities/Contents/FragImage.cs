using System.Text.RegularExpressions;

namespace AioTieba4DotNet.Api.Entities.Contents;

/// <summary>
///     图像碎片
/// </summary>
public partial class FragImage : IFrag
{
    private static readonly Regex ImageHashExp = MyRegex();

    /// <summary>
    ///     小图链接 宽720px 一定是静态图
    /// </summary>
    public string Src { get; init; } = "";

    /// <summary>
    ///     大图链接 宽960px
    /// </summary>
    public string BigSrc { get; init; } = "";

    /// <summary>
    ///     原图链接
    /// </summary>
    public string OriginSrc { get; init; } = "";

    /// <summary>
    ///     原图大小
    /// </summary>
    public uint OriginSize { get; init; }

    /// <summary>
    ///     图像在客户端预览显示的宽度
    /// </summary>
    public int ShowWidth { get; init; }

    /// <summary>
    ///     图像在客户端预览显示的高度
    /// </summary>
    public int ShowHeight { get; init; }

    /// <summary>
    ///     百度图床hash
    /// </summary>
    public string Hash { get; init; } = "";

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
        return "FragImage";
    }

    /// <summary>
    ///     转换为字典用于序列化
    /// </summary>
    /// <returns>包含碎片数据的字典</returns>
    public Dictionary<string, object> ToDict()
    {
        return new Dictionary<string, object>
        {
            { "type", "3" },
            { "src", Src },
            { "origin_src", OriginSrc },
            { "show_width", ShowWidth },
            { "show_height", ShowHeight }
        };
    }

    [GeneratedRegex("/([a-z0-9]{32,})\\.")]
    private static partial Regex MyRegex();

    /// <summary>
    ///     从贴吧原始数据转换
    /// </summary>
    /// <param name="dataProto">Protobuf 碎片数据</param>
    /// <returns>图像碎片实体</returns>
    internal static FragImage FromTbData(PbContent dataProto)
    {
        var src = dataProto.CdnSrc;
        var bigSrc = dataProto.BigCdnSrc;
        var originSrc = dataProto.OriginSrc;
        var originSize = dataProto.OriginSize;

        var bSize = dataProto.Bsize.Split(',');
        var showWidth = 0;
        var showHeight = 0;
        if (bSize.Length >= 2)
        {
            _ = int.TryParse(bSize[0], out showWidth);
            _ = int.TryParse(bSize[1], out showHeight);
        }

        var hash = ImageHashExp.Match(src).Groups[1].Value;

        return new FragImage
        {
            Src = src,
            BigSrc = bigSrc,
            OriginSrc = originSrc,
            OriginSize = originSize,
            ShowWidth = showWidth,
            ShowHeight = showHeight,
            Hash = hash
        };
    }

    /// <summary>
    ///     从贴吧原始数据转换
    /// </summary>
    /// <param name="dataProto">Protobuf 媒体数据</param>
    /// <returns>图像碎片实体</returns>
    internal static FragImage FromTbData(Media dataProto)
    {
        var src = dataProto.SmallPic;
        var bigSrc = dataProto.BigPic;
        var originSrc = dataProto.OriginPic;
        var originSize = dataProto.OriginSize;

        var showWidth = (int)dataProto.Width;
        var showHeight = (int)dataProto.Height;

        var hash = ImageHashExp.Match(src).Groups[1].Value;

        return new FragImage
        {
            Src = src,
            BigSrc = bigSrc,
            OriginSrc = originSrc,
            OriginSize = originSize,
            ShowWidth = showWidth,
            ShowHeight = showHeight,
            Hash = hash
        };
    }

    /// <summary>
    ///     格式设置成员
    /// </summary>
    /// <returns>string</returns>
    public override string ToString()
    {
        return
            $"{GetFragType()} {nameof(Src)}: {Src}, {nameof(BigSrc)}: {BigSrc}, {nameof(OriginSrc)}: {OriginSrc}, {nameof(OriginSize)}: {OriginSize}, {nameof(ShowWidth)}: {ShowWidth}, {nameof(ShowHeight)}: {ShowHeight}, {nameof(Hash)}: {Hash}";
    }
}
