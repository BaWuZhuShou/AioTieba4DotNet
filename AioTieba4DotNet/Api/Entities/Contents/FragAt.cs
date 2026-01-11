namespace AioTieba4DotNet.Api.Entities.Contents;

/// <summary>
/// @碎片
/// </summary>
public class FragAt : IFrag
{
    /// <summary>
    /// 被@用户的昵称 含@
    /// </summary>
    public string Text { get; init; } = "";

    /// <summary>
    /// 被@用户的user_id
    /// </summary>
    public long UserId { get; init; }

    /// <summary>
    /// 从贴吧原始数据转换
    /// </summary>
    /// <param name="dataProto">Protobuf 碎片数据</param>
    /// <returns>@碎片实体</returns>
    public static FragAt FromTbData(PbContent dataProto)
    {
        var text = dataProto.Text;
        var userId = dataProto.Uid;
        return new FragAt { Text = text, UserId = userId };
    }

    /// <summary>
    /// 获取碎片类型
    /// </summary>
    /// <returns>碎片类型名称</returns>
    public string GetFragType()
    {
        return "FragAt";
    }

    /// <summary>
    /// 转换为字典用于序列化
    /// </summary>
    /// <returns>包含碎片数据的字典</returns>
    public Dictionary<string, object> ToDict()
    {
        return new Dictionary<string, object> { { "type", "4" }, { "uid", UserId }, { "text", Text } };
    }

    /// <summary>
    /// 格式设置成员
    /// </summary>
    /// <returns>string</returns>
    public override string ToString()
    {
        return $"{GetFragType()} {nameof(Text)}: {Text}, {nameof(UserId)}: {UserId}";
    }
}
