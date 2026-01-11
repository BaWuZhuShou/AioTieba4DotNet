namespace AioTieba4DotNet.Api.Entities.Contents;

/// <summary>
/// 表情碎片
/// </summary>
public class FragEmoji : IFrag
{
    /// <summary>
    /// 表情图片id
    /// </summary>
    public string Id { get; init; } = "";

    /// <summary>
    /// 表情描述
    /// </summary>
    public string Desc { get; init; } = "";

    /// <summary>
    /// 文本内容
    /// </summary>
    public string Text => "";

    /// <summary>
    /// 从贴吧原始数据转换
    /// </summary>
    /// <param name="dataProto">Protobuf 碎片数据</param>
    /// <returns>表情碎片实体</returns>
    public static FragEmoji FromTbData(PbContent dataProto)
    {
        var id = dataProto.Text;
        var desc = dataProto.C;
        return new FragEmoji { Id = id, Desc = desc };
    }

    /// <summary>
    /// 获取碎片类型
    /// </summary>
    /// <returns>碎片类型名称</returns>
    public string GetFragType()
    {
        return "FragEmoji";
    }

    /// <summary>
    /// 转换为字典用于序列化
    /// </summary>
    /// <returns>包含碎片数据的字典</returns>
    public Dictionary<string, object> ToDict()
    {
        return new Dictionary<string, object> { { "type", "2" }, { "text", Id } };
    }

    /// <summary>
    /// 格式设置成员
    /// </summary>
    /// <returns>string</returns>
    public override string ToString()
    {
        return $"{GetFragType()} {nameof(Id)}: {Id}, {nameof(Desc)}: {Desc}";
    }
}
