using System.Net;

namespace AioTieba4DotNet.Core;

public record HttpContainer(Dictionary<string, string> Headers, CookieContainer Cookies);
