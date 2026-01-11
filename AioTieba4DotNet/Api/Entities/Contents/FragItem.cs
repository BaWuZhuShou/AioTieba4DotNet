namespace AioTieba4DotNet.Api.Entities.Contents;

/// <summary>
/// item碎片
/// </summary>
public class FragItem : IFrag
{
    /// <summary>
    /// item名称
    /// </summary>
    public string Text { get; init; } = "";

    /// <summary>
    /// 从贴吧原始数据转换
    /// </summary>
    /// <param name="dataProto">Protobuf 碎片数据</param>
    /// <returns>item碎片实体</returns>
    public static FragItem FromTbData(PbContent dataProto)
    {
        var text = dataProto.Item.ItemName;
        return new FragItem { Text = text };
    }

    /// <summary>
    /// 获取碎片类型
    /// </summary>
    /// <returns>碎片类型名称</returns>
    public string GetFragType()
    {
        return "FragItem";
    }

    /// <summary>
    /// 转换为字典用于序列化
    /// </summary>
    /// <returns>包含碎片数据的字典</returns>
    public Dictionary<string, object> ToDict()
    {
        return new Dictionary<string, object>();
    }

    /// <summary>
    /// 格式设置成员
    /// </summary>
    /// <returns>string</returns>
    public override string ToString()
    {
        return $"{GetFragType()} {nameof(Text)}: {Text}";
    }
}
