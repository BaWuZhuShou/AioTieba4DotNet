using System.Net;
using System.Text;

namespace AioTieba4DotNet.Transport.Http;

internal static class TiebaHttpClientFactory
{
    internal static HttpClient CreateClient()
    {
        EnsureEncodingProviderRegistered();
        return new HttpClient(CreatePrimaryHandler()) { Timeout = Timeout.InfiniteTimeSpan };
    }

    internal static HttpClientHandler CreatePrimaryHandler()
    {
        EnsureEncodingProviderRegistered();
        return new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new CookieContainer(),
            AutomaticDecompression = DecompressionMethods.GZip
        };
    }

    internal static void ConfigureNamedClient(HttpClient client)
    {
        EnsureEncodingProviderRegistered();
        client.Timeout = Timeout.InfiniteTimeSpan;
    }

    internal static void EnsureEncodingProviderRegistered()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }
}
