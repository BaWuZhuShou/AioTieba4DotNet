using System.Web;

namespace AioTieba4DotNet.Models.Contents;

/// <summary>
///     链接碎片
/// </summary>
public class FragLink : IFrag
{
    /// <summary>
    ///     链接标题
    /// </summary>
    public string Title { get; init; } = "";

    /// <summary>
    ///     解析后的原链接
    /// </summary>
    public required Uri RawUrl { get; init; }

    /// <summary>
    ///     解析后的去前缀链接
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

    /// <summary>
    ///     是否外部链接
    /// </summary>
    public bool IsExternal => RawUrl.AbsolutePath == "/mo/q/checkurl";

    /// <summary>
    ///     原链接
    /// </summary>
    public override string Text { get; init; } = "";

    /// <summary>
    ///     获取碎片类型
    /// </summary>
    /// <returns>碎片类型名称</returns>
    public override string GetFragType()
    {
        return "FragLink";
    }

    /// <summary>
    ///     转换为字典用于序列化
    /// </summary>
    /// <returns>包含碎片数据的字典</returns>
    public override Dictionary<string, object> ToDict()
    {
        return new Dictionary<string, object> { { "type", "1" }, { "link", RawUrl }, { "text", Text } };
    }

    private string GetQueryParam(string key)
    {
        var query = RawUrl.Query;
        var queryParams = HttpUtility.ParseQueryString(query);
        return queryParams[key] ?? string.Empty;
    }

    /// <summary>
    ///     格式设置成员
    /// </summary>
    /// <returns>string</returns>
    public override string ToString()
    {
        return
            $"{GetFragType()} {nameof(Text)}: {Text}, {nameof(Title)}: {Title}, {nameof(RawUrl)}: {RawUrl}, {nameof(Url)}: {Url}, {nameof(IsExternal)}: {IsExternal}";
    }
}
