namespace AioTieba4DotNet.Models.Contents;

/// <summary>
///     表情碎片
/// </summary>
public class FragEmoji : IFrag
{
    /// <summary>
    ///     表情图片id
    /// </summary>
    public string Id { get; init; } = "";

    /// <summary>
    ///     表情描述
    /// </summary>
    public string Desc { get; init; } = "";

    /// <summary>
    ///     获取碎片类型
    /// </summary>
    /// <returns>碎片类型名称</returns>
    public override string GetFragType()
    {
        return "FragEmoji";
    }

    /// <summary>
    ///     转换为字典用于序列化
    /// </summary>
    /// <returns>包含碎片数据的字典</returns>
    public override Dictionary<string, object> ToDict()
    {
        return new Dictionary<string, object> { { "type", "2" }, { "text", Id } };
    }
    /// <summary>
    ///     格式设置成员
    /// </summary>
    /// <returns>string</returns>
    public override string ToString()
    {
        return $"{GetFragType()} {nameof(Id)}: {Id}, {nameof(Desc)}: {Desc}";
    }
}
