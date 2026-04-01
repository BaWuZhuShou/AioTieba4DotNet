using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Transport;

namespace AioTieba4DotNet.Api.GetImages;

[PythonApi("aiotieba.api.get_images")]
internal sealed class GetImages(ITiebaHttpCore httpCore)
{
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
        request.Headers.Referrer = new Uri("https://tieba.baidu.com/");

        using var response = await httpCore.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new TiebaTransportException($"Image request failed with status code {(int)response.StatusCode}.");

        var contentType = response.Content.Headers.ContentType?.MediaType;
        var data = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return (data, contentType);
    }
}
