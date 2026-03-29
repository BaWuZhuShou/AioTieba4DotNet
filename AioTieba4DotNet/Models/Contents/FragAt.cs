namespace AioTieba4DotNet.Models.Contents;

/// <summary>
///     @碎片
/// </summary>
public class FragAt : IFrag
{
    /// <summary>
    ///     被@用户的user_id
    /// </summary>
    public long UserId { get; init; }

    /// <summary>
    ///     被@用户的昵称 含@
    /// </summary>
    public override string Text { get; init; } = "";

    /// <summary>
    ///     获取碎片类型
    /// </summary>
    /// <returns>碎片类型名称</returns>
    public override string GetFragType()
    {
        return "FragAt";
    }

    /// <summary>
    ///     转换为字典用于序列化
    /// </summary>
    /// <returns>包含碎片数据的字典</returns>
    public override Dictionary<string, object> ToDict()
    {
        return new Dictionary<string, object> { { "type", "4" }, { "uid", UserId }, { "text", Text } };
    }
    /// <summary>
    ///     格式设置成员
    /// </summary>
    /// <returns>string</returns>
    public override string ToString()
    {
        return $"{GetFragType()} {nameof(Text)}: {Text}, {nameof(UserId)}: {UserId}";
    }
}
