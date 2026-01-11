namespace AioTieba4DotNet.Api.Entities.Contents;

/// <summary>
/// 纯文本碎片
/// </summary>
public class FragText : IFrag
{
    /// <summary>
    /// 文本内容
    /// </summary>
    public string Text { get; init; } = "";

    /// <summary>
    /// 从贴吧原始数据转换
    /// </summary>
    /// <param name="dataProto">Protobuf 碎片数据</param>
    /// <returns>纯文本碎片实体</returns>
    public static FragText FromTbData(PbContent dataProto)
    {
        var text = dataProto.Text;
        return new FragText { Text = text };
    }

    /// <summary>
    /// 从贴吧原始数据转换
    /// </summary>
    /// <param name="dataProto">Protobuf 摘要数据</param>
    /// <returns>纯文本碎片实体</returns>
    public static FragText FromTbData(PostInfoList.Types.PostInfoContent.Types.Abstract dataProto)
    {
        var text = dataProto.Text;
        return new FragText { Text = text };
    }


    /// <summary>
    /// 获取碎片类型
    /// </summary>
    /// <returns>碎片类型名称</returns>
    public string GetFragType()
    {
        return "FragText";
    }

    /// <summary>
    /// 转换为字典用于序列化
    /// </summary>
    /// <returns>包含碎片数据的字典</returns>
    public Dictionary<string, object> ToDict()
    {
        return new Dictionary<string, object> { { "type", "0" }, { "text", Text } };
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
