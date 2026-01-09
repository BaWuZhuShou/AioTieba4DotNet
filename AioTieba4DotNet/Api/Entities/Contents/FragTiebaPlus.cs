namespace AioTieba4DotNet.Api.Entities.Contents;

/// <summary>
/// 贴吧plus广告碎片
/// </summary>
public class FragTiebaPlus : IFrag
{
    /// <summary>
    /// 贴吧plus广告描述
    /// </summary>
    public string Text { get; init; } = "";

    /// <summary>
    /// 解析后的贴吧plus广告跳转链接
    /// </summary>
    public required Uri Url { get; init; }

    /// <summary>
    /// 从贴吧原始数据转换
    /// </summary>
    /// <param name="dataProto"></param>
    /// <returns>FragTiebaPlus</returns>
    public static FragTiebaPlus FromTbData(PbContent dataProto)
    {
        var text = dataProto.TiebaplusInfo.Desc;
        var url = new Uri(dataProto.TiebaplusInfo.JumpUrl);
        return new FragTiebaPlus { Text = text, Url = url };
    }

    /// <summary>
    /// 碎片类型
    /// </summary>
    /// <returns>string</returns>
    public string GetFragType()
    {
        return "FragTiebaPlus";
    }

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
        return $"{GetFragType()} {nameof(Text)}: {Text}, {nameof(Url)}: {Url}";
    }
}
