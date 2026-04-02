using AioTieba4DotNet.Internal;

namespace AioTieba4DotNet.Transport.Http;

internal static class TiebaHttpRequestSigner
{
    internal static List<KeyValuePair<string, string>> Sign(IEnumerable<KeyValuePair<string, string>> items)
    {
        var list = items.Select(item => new KeyValuePair<string, string>(item.Key, item.Value)).ToList();
        list.Add(new KeyValuePair<string, string>("sign", Signer.Sign(list)));
        return list;
    }
}
