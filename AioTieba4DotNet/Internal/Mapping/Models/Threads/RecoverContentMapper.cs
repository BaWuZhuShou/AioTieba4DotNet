using System.Text.RegularExpressions;
using AioTieba4DotNet.Models.Contents;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Internal.Mapping;

internal static partial class RecoverContentMapper
{
    internal static Content FromTbData(JObject? data)
    {
        if (data is null)
            return new Content();

        var texts = new List<FragText>();
        var images = new List<FragImage>();
        var frags = new List<IFrag>();

        if (data.GetValue("content_detail") is JArray contentDetail)
            foreach (var item in contentDetail.OfType<JObject>())
            {
                if (item.GetValue("type")?.Value<int>() != 1)
                    continue;

                var text = new FragText { Text = item.GetValue("value")?.Value<string>() ?? string.Empty };
                texts.Add(text);
                frags.Add(text);
            }

        if (data.GetValue("all_pics") is JArray pictureArray)
            foreach (var picture in pictureArray.OfType<JObject>())
            {
                var src = picture.GetValue("url")?.Value<string>() ?? string.Empty;
                var image = new FragImage
                {
                    Src = src,
                    ShowWidth = picture.GetValue("width")?.Value<int>() ?? 0,
                    ShowHeight = picture.GetValue("height")?.Value<int>() ?? 0,
                    Hash = ExtractHash(src)
                };
                images.Add(image);
                frags.Add(image);
            }

        for (var index = 0; index < frags.Count; index++)
            frags[index].Index = index;

        return new Content { Texts = texts, Images = images, Frags = frags };
    }

    private static string ExtractHash(string src)
    {
        var match = ImageHashRegex().Match(src);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    [GeneratedRegex("/([a-z0-9]{32,})\\.", RegexOptions.IgnoreCase)]
    private static partial Regex ImageHashRegex();
}
