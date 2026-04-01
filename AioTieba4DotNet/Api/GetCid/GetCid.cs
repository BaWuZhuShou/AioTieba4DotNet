using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetCid;

[RequireBduss]
[PythonApi("aiotieba.api.get_cid")]
internal class GetCid(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    public async Task<int> RequestAsync(string fname, string cname, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cname))
            return 0;

        var data = new List<KeyValuePair<string, string>> { new("BDUSS", HttpCore.Account!.Bduss), new("word", fname) };

        var requestUri = new UriBuilder(Const.AppInsecureScheme, Const.AppBaseHost, 80, "/c/c/bawu/goodlist").Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        var body = ParseBody(result);
        var categoriesToken = body["cates"];
        if (categoriesToken is null)
            return 0;

        foreach (var category in categoriesToken.Children<JObject>())
        {
            var className = category["class_name"]?.ToObject<string>();
            if (string.Equals(className, cname, StringComparison.Ordinal))
                return category["class_id"]?.ToObject<int?>() ?? 0;
        }

        return 0;
    }
}
