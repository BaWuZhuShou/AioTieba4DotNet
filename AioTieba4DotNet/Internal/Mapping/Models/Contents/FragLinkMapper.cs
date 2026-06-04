using AioTieba4DotNet.Models.Contents;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class FragLinkMapper
{
    internal static FragLink FromTbData(PbContent dataProto)

    {
        var text = dataProto.Link;

        var title = dataProto.Text;

        var rawUrl = CreateRawUrl(text);

        return new FragLink { Text = text, Title = title, RawUrl = rawUrl };
    }


    internal static FragLink FromTbData(PostInfoList.Types.PostInfoContent.Types.Abstract dataProto)

    {
        var text = dataProto.Link;

        var title = dataProto.Text;

        var rawUrl = CreateRawUrl(text);

        return new FragLink { Text = text, Title = title, RawUrl = rawUrl };
    }

    private static Uri CreateRawUrl(string? text)
    {
        return Uri.TryCreate(text, UriKind.Absolute, out var rawUrl)
            ? rawUrl
            : new Uri("about:blank");
    }
}
