using System;
using AioTieba4DotNet.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests;

public abstract class TestBase
{
    static TestBase()
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", true)
            .AddJsonFile("appsettings.test.json", true)
            .AddEnvironmentVariables("TIEBA_")
            .Build();

        // 优先从环境变量读取 (TIEBA_BDUSS, TIEBA_STOKEN)
        // 其次从配置文件读取 (TieBa:BDUSS, TieBa:STOKEN)
        Bduss = Configuration["BDUSS"] ?? Configuration["TieBa:BDUSS"] ?? string.Empty;
        Stoken = Configuration["STOKEN"] ?? Configuration["TieBa:STOKEN"] ?? string.Empty;
    }

    protected TestBase()
    {
        var account = new Account(Bduss, Stoken);
        HttpCore = new HttpCore();
        HttpCore.SetAccount(account);
        WebsocketCore = new WebsocketCore(account);
        Client = new TiebaClient(HttpCore);
    }

    protected static IConfiguration Configuration { get; }
    protected static string Bduss { get; }
    protected static string Stoken { get; }

    protected HttpCore HttpCore { get; }
    protected WebsocketCore WebsocketCore { get; }
    protected TiebaClient Client { get; }

    protected bool IsAuthenticated => !string.IsNullOrEmpty(Bduss);

    protected void EnsureAuthenticated()
    {
        if (!IsAuthenticated) Assert.Inconclusive("Skipping test: BDUSS is not configured.");
    }
}
