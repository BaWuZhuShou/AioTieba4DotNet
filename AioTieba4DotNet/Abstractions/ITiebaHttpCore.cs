using AioTieba4DotNet.Core;

namespace AioTieba4DotNet.Abstractions;

public interface ITiebaHttpCore
{
    Account? Account { get; }
    HttpClient HttpClient { get; }
    void SetAccount(Account newAccount);
    Task<HttpResponseMessage> PackAppFormRequestAsync(Uri uri, List<KeyValuePair<string, string>> data);
    Task<HttpResponseMessage> PackProtoRequestAsync(Uri uri, byte[] data);
    Task<HttpResponseMessage> PackWebGetRequestAsync(Uri uri, List<KeyValuePair<string, string>> parameters);
    Task<HttpResponseMessage> PackWebFormRequestAsync(Uri uri, List<KeyValuePair<string, string>> data);
}
