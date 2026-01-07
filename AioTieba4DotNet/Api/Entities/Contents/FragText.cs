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
    /// <param name="dataProto"></param>
    /// <returns>FragText</returns>
    public static FragText FromTbData(PbContent dataProto)
    {
        var text = dataProto.Text;
        return new FragText { Text = text };
    }
    /// <summary>
    /// 从贴吧原始数据转换
    /// </summary>
    /// <param name="dataProto"></param>
    /// <returns></returns>
    public static FragText FromTbData(PostInfoList.Types.PostInfoContent.Types.Abstract dataProto)
    {
        var text = dataProto.Text;
        return new FragText { Text = text };
    }


    /// <summary>
    /// 碎片类型
    /// </summary>
    /// <returns>string</returns>
    public string GetFragType()
    {
        return "FragText";
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