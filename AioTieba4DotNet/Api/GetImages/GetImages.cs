using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.GetImages;

internal sealed class GetImages(ITiebaHttpCore httpCore)
{
    private const string TiebaReferrer = "tieba.baidu.com";

    public async Task<ForumImageBytes> RequestBytesAsync(Uri imageUri, CancellationToken cancellationToken = default)
    {
        var (data, contentType) = await RequestCoreAsync(imageUri, cancellationToken);
        return ForumImageMapper.ToBytes(data, contentType);
    }

    public async Task<ForumImage> RequestAsync(Uri imageUri, CancellationToken cancellationToken = default)
    {
        var (data, contentType) = await RequestCoreAsync(imageUri, cancellationToken);
        return ForumImageMapper.ToImage(data, contentType);
    }

    private async Task<(byte[] Data, string? ContentType)> RequestCoreAsync(Uri imageUri,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, imageUri);
        request.Headers.TryAddWithoutValidation("User-Agent", $"aiotieba/{AioTieba4DotNet.Internal.Const.Version}");
        request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
        request.Headers.TryAddWithoutValidation("Cache-Control", "no-cache");
        request.Headers.TryAddWithoutValidation("Connection", "keep-alive");
        request.Headers.TryAddWithoutValidation("Referer", TiebaReferrer);

        using var response = await httpCore.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new TiebaTransportException($"Image request failed with status code {(int)response.StatusCode}.");

        var contentType = response.Content.Headers.ContentType?.MediaType;
        var data = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return (data, contentType);
    }
}
