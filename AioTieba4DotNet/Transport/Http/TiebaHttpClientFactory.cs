using System.Net;
using System.Text;

namespace AioTieba4DotNet.Transport.Http;

internal static class TiebaHttpClientFactory
{
    internal static HttpClient CreateClient()
    {
        EnsureEncodingProviderRegistered();
        return new HttpClient(CreateParityHandlerPipeline(), disposeHandler: true) { Timeout = Timeout.InfiniteTimeSpan };
    }

    internal static HttpMessageHandler CreateParityHandlerPipeline()
    {
        EnsureEncodingProviderRegistered();
        return new TiebaHttpParityHandler(CreatePrimaryHandler());
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
