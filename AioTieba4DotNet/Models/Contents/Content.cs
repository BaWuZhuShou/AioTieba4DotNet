namespace AioTieba4DotNet.Models.Contents;

/// <summary>
///     内容碎片列表
/// </summary>
public class Content
{
    /// <summary>
    ///     文本内容
    /// </summary>
    public string Text => string.Concat(Frags.Select(f => f.Text));

    /// <summary>
    ///     纯文本碎片列表
    /// </summary>
    public List<FragText> Texts { get; init; } = [];

    /// <summary>
    ///     表情碎片列表
    /// </summary>
    public List<FragEmoji> Emojis { get; init; } = [];

    /// <summary>
    ///     图像碎片列表
    /// </summary>
    public List<FragImage> Images { get; init; } = [];

    /// <summary>
    ///     @碎片列表
    /// </summary>
    public List<FragAt> Ats { get; init; } = [];

    /// <summary>
    ///     链接碎片列表
    /// </summary>
    public List<FragLink> Links { get; init; } = [];

    /// <summary>
    ///     贴吧plus碎片列表
    /// </summary>
    public List<FragTiebaPlus> TiebaPluses { get; init; } = [];

    /// <summary>
    ///     视频碎片
    /// </summary>
    public FragVideo? Video { get; set; }

    /// <summary>
    ///     音频碎片
    /// </summary>
    public FragVoice? Voice { get; set; }

    /// <summary>
    ///     所有原始碎片
    /// </summary>
    public List<IFrag> Frags { get; init; } = [];
/// <summary>
    ///     格式设置
    /// </summary>
    /// <returns>string</returns>
    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(nameof(Text)).Append(": ").Append(Text);

        if (Emojis.Count > 0) sb.Append(", ").Append(nameof(Emojis)).Append(": [").Append(string.Join(", ", Emojis)).Append(']');
        if (Images.Count > 0) sb.Append(", ").Append(nameof(Images)).Append(": [").Append(string.Join(", ", Images)).Append(']');
        if (Ats.Count > 0) sb.Append(", ").Append(nameof(Ats)).Append(": [").Append(string.Join(", ", Ats)).Append(']');
        if (Links.Count > 0) sb.Append(", ").Append(nameof(Links)).Append(": [").Append(string.Join(", ", Links)).Append(']');
        if (TiebaPluses.Count > 0) sb.Append(", ").Append(nameof(TiebaPluses)).Append(": [").Append(string.Join(", ", TiebaPluses)).Append(']');
        if (Video != null) sb.Append(", ").Append(nameof(Video)).Append(": ").Append(Video);
        if (Voice != null) sb.Append(", ").Append(nameof(Voice)).Append(": ").Append(Voice);
        if (Frags.Count > 0) sb.Append(", ").Append(nameof(Frags)).Append(": [").Append(string.Join(", ", Frags)).Append(']');

        return sb.ToString();
    }

    private static void SetFragsIndex(List<IFrag> frags)
    {
        for (var i = 0; i < frags.Count; i++) frags[i].Index = i;
    }
}
