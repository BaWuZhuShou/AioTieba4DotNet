namespace AioTieba4DotNet.Models.Contents;

/// <summary>
///     纯文本碎片
/// </summary>
public class FragText : IFrag
{
    /// <summary>
    ///     文本内容
    /// </summary>
    public override string Text { get; init; } = "";


    /// <summary>
    ///     获取碎片类型
    /// </summary>
    /// <returns>碎片类型名称</returns>
    public override string GetFragType()
    {
        return "FragText";
    }

    /// <summary>
    ///     转换为字典用于序列化
    /// </summary>
    /// <returns>包含碎片数据的字典</returns>
    public override Dictionary<string, object> ToDict()
    {
        return new Dictionary<string, object> { { "type", "0" }, { "text", Text } };
    }

    /// <summary>
    ///     格式设置成员
    /// </summary>
    /// <returns>string</returns>
    public override string ToString()
    {
        return $"{GetFragType()} {nameof(Text)}: {Text}";
    }
}
