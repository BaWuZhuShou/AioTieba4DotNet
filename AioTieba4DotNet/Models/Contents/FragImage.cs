using System.Text.RegularExpressions;

namespace AioTieba4DotNet.Models.Contents;

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
    ///     获取碎片类型
    /// </summary>
    /// <returns>碎片类型名称</returns>
    public override string GetFragType()
    {
        return "FragImage";
    }

    /// <summary>
    ///     转换为字典用于序列化
    /// </summary>
    /// <returns>包含碎片数据的字典</returns>
    public override Dictionary<string, object> ToDict()
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
    ///     格式设置成员
    /// </summary>
    /// <returns>string</returns>
    public override string ToString()
    {
        return
            $"{GetFragType()} {nameof(Src)}: {Src}, {nameof(BigSrc)}: {BigSrc}, {nameof(OriginSrc)}: {OriginSrc}, {nameof(OriginSize)}: {OriginSize}, {nameof(ShowWidth)}: {ShowWidth}, {nameof(ShowHeight)}: {ShowHeight}, {nameof(Hash)}: {Hash}";
    }
}
