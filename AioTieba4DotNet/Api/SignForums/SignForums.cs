using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Transport;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.SignForums;

internal sealed class SignForums(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    public async Task<bool> RequestAsync(CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("_client_version", Const.MainVersion), new("subapp_type", "hybrid")
        };

        var requestUri = new UriBuilder("http", Const.WebBaseHost, 80, "/c/c/forum/msign").Uri;
        var result = await HttpCore.SendAsync(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Add("Subapp-Type", "hybrid");
            request.Content = new FormUrlEncodedContent(data);
            return request;
        }, cancellationToken: cancellationToken);

        ValidateNestedError(ParseBody(result));
        return true;
    }

    private static void ValidateNestedError(JObject body)
    {
        if (body.GetValue("error") is not JObject error)
            return;

        var errno = error.Value<int?>("errno") ?? 0;
        if (errno != 0)
            throw new TieBaServerException(errno, error.Value<string>("errmsg") ?? string.Empty);
    }
}
