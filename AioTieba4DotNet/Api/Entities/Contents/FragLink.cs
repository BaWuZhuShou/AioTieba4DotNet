namespace AioTieba4DotNet.Api.Entities.Contents;

/// <summary>
/// 链接碎片
/// </summary>
public class FragLink : IFrag
{
    /// <summary>
    /// 原链接
    /// </summary>
    public string Text { get; init; } = "";

    /// <summary>
    /// 链接标题
    /// </summary>
    public string Title { get; init; } = "";

    /// <summary>
    /// 解析后的原链接
    /// </summary>
    public required Uri RawUrl { get; init; }

    /// <summary>
    /// 解析后的去前缀链接
    /// </summary>
    public Uri Url
    {
        get
        {
            if (!IsExternal) return RawUrl;
            var urlParam = GetQueryParam("url");
            return string.IsNullOrEmpty(urlParam) ? RawUrl : new Uri(urlParam);
        }
    }

    private string GetQueryParam(string key)
    {
        var query = RawUrl.Query;
        var queryParams = System.Web.HttpUtility.ParseQueryString(query);
        return queryParams[key] ?? string.Empty;
    }

    /// <summary>
    /// 是否外部链接
    /// </summary>
    public bool IsExternal => RawUrl.AbsolutePath == "/mo/q/checkurl";

    /// <summary>
    /// 从贴吧原始数据转换
    /// </summary>
    /// <param name="dataProto">Protobuf 碎片数据</param>
    /// <returns>链接碎片实体</returns>
    public static FragLink FromTbData(PbContent dataProto)
    {
        var text = dataProto.Link;
        var title = dataProto.Text;
        var rawUrl = new Uri(text);
        return new FragLink { Text = text, Title = title, RawUrl = rawUrl };
    }

    /// <summary>
    /// 从贴吧原始数据转换
    /// </summary>
    /// <param name="dataProto">Protobuf 摘要数据</param>
    /// <returns>链接碎片实体</returns>
    public static FragLink FromTbData(PostInfoList.Types.PostInfoContent.Types.Abstract dataProto)
    {
        var text = dataProto.Link;
        var title = dataProto.Text;
        var rawUrl = new Uri(text);
        return new FragLink { Text = text, Title = title, RawUrl = rawUrl };
    }

    /// <summary>
    /// 获取碎片类型
    /// </summary>
    /// <returns>碎片类型名称</returns>
    public string GetFragType()
    {
        return "FragLink";
    }

    /// <summary>
    /// 转换为字典用于序列化
    /// </summary>
    /// <returns>包含碎片数据的字典</returns>
    public Dictionary<string, object> ToDict()
    {
        return new Dictionary<string, object> { { "type", "1" }, { "link", RawUrl }, { "text", Text } };
    }

    /// <summary>
    /// 格式设置成员
    /// </summary>
    /// <returns>string</returns>
    public override string ToString()
    {
        return
            $"{GetFragType()} {nameof(Text)}: {Text}, {nameof(Title)}: {Title}, {nameof(RawUrl)}: {RawUrl}, {nameof(Url)}: {Url}, {nameof(IsExternal)}: {IsExternal}";
    }
}
