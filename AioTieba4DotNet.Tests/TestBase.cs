#nullable enable
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
            .AddJsonFile("appsettings.fixtures.json", true)
            .AddEnvironmentVariables("TIEBA_")
            .Build();

        // 优先从环境变量读取 (TIEBA_BDUSS, TIEBA_STOKEN)
        // 其次从配置文件读取 (TieBa:BDUSS, TieBa:STOKEN)
        Bduss = Configuration["BDUSS"] ?? Configuration["TieBa:BDUSS"] ?? string.Empty;
        Stoken = Configuration["STOKEN"] ?? Configuration["TieBa:STOKEN"] ?? string.Empty;
        ConfiguredSafeForumQuery = Configuration["SAFEFORUMQUERY"] ?? Configuration["TieBa:SafeForumQuery"] ?? "lol欧服吧";
        ConfiguredCanonicalSafeForumName = Configuration["SAFEFORUMNAME"] ?? Configuration["TieBa:SafeForumName"] ?? "lol欧服";
        OwnedThreadId = ReadOptionalLong("OWNEDTHREADID", "TieBa:OwnedThreadId");
        OwnedReplyId = ReadOptionalLong("OWNEDREPLYID", "TieBa:OwnedReplyId");
        SafeTargetPortrait = ReadOptionalString("SAFETARGETPORTRAIT", "TieBa:SafeTargetPortrait");
        SafeTargetUserId = ReadOptionalLong("SAFETARGETUSERID", "TieBa:SafeTargetUserId");
        SafeMessageRecipient = ReadOptionalString("SAFEMESSAGERECIPIENT", "TieBa:SafeMessageRecipient");
        SafeChatroomId = ReadOptionalLong("SAFECHATROOMID", "TieBa:SafeChatroomId");
    }

    protected TestBase()
    {
        var account = new Account(Bduss, Stoken);
        HttpCore = new HttpCore();
        HttpCore.SetAccount(account);
        WebsocketCore = new WebsocketCore(account);
        Client = new TiebaClient(new TiebaOptions
        {
            Bduss = string.IsNullOrWhiteSpace(Bduss) ? null : Bduss,
            Stoken = string.IsNullOrWhiteSpace(Stoken) ? null : Stoken
        });
    }

    protected static IConfiguration Configuration { get; }
    protected static string Bduss { get; }
    protected static string Stoken { get; }
    protected static string ConfiguredSafeForumQuery { get; }
    protected static string ConfiguredCanonicalSafeForumName { get; }
    protected static long? OwnedThreadId { get; }
    protected static long? OwnedReplyId { get; }
    protected static string? SafeTargetPortrait { get; }
    protected static long? SafeTargetUserId { get; }
    protected static string? SafeMessageRecipient { get; }
    protected static long? SafeChatroomId { get; }

    private protected HttpCore HttpCore { get; }
    private protected WebsocketCore WebsocketCore { get; }
    protected TiebaClient Client { get; }

    protected bool IsAuthenticated => !string.IsNullOrEmpty(Bduss);

    protected void EnsureAuthenticated()
    {
        if (!IsAuthenticated) Assert.Inconclusive("Skipping test: BDUSS is not configured.");
    }

    protected long RequireOwnedThreadFixture(string operationName)
    {
        EnsureAuthenticated();
        if (OwnedThreadId is not > 0)
            Assert.Inconclusive(
                $"Skipping {operationName}: no owned thread fixture is configured. Set TIEBA_OWNEDTHREADID or TieBa:OwnedThreadId for the safe forum {ConfiguredCanonicalSafeForumName}.");

        return OwnedThreadId.Value;
    }

    protected long RequireOwnedReplyFixture(string operationName)
    {
        EnsureAuthenticated();
        if (OwnedReplyId is not > 0)
            Assert.Inconclusive(
                $"Skipping {operationName}: no owned reply fixture is configured. Set TIEBA_OWNEDREPLYID or TieBa:OwnedReplyId for the safe forum {ConfiguredCanonicalSafeForumName}.");

        return OwnedReplyId.Value;
    }

    protected string RequireSafeTargetPortraitFixture(string operationName)
    {
        EnsureAuthenticated();
        if (string.IsNullOrWhiteSpace(SafeTargetPortrait))
            Assert.Inconclusive(
                $"Skipping {operationName}: no safe target portrait is configured. Set TIEBA_SAFETARGETPORTRAIT or TieBa:SafeTargetPortrait.");

        return SafeTargetPortrait;
    }

    protected long RequireSafeTargetUserIdFixture(string operationName)
    {
        EnsureAuthenticated();
        if (SafeTargetUserId is not > 0)
            Assert.Inconclusive(
                $"Skipping {operationName}: no safe target user id is configured. Set TIEBA_SAFETARGETUSERID or TieBa:SafeTargetUserId.");

        return SafeTargetUserId.Value;
    }

    protected string RequireSafeMessageRecipientFixture(string operationName)
    {
        EnsureAuthenticated();
        if (string.IsNullOrWhiteSpace(SafeMessageRecipient))
            Assert.Inconclusive(
                $"Skipping {operationName}: no safe message recipient is configured. Set TIEBA_SAFEMESSAGERECIPIENT or TieBa:SafeMessageRecipient.");

        return SafeMessageRecipient;
    }

    protected long RequireSafeChatroomIdFixture(string operationName)
    {
        EnsureAuthenticated();
        if (SafeChatroomId is not > 0)
            Assert.Inconclusive(
                $"Skipping {operationName}: no safe chatroom id is configured. Set TIEBA_SAFECHATROOMID or TieBa:SafeChatroomId.");

        return SafeChatroomId.Value;
    }

    private static long? ReadOptionalLong(string environmentKey, string configurationKey)
    {
        var raw = Configuration[environmentKey] ?? Configuration[configurationKey];
        return long.TryParse(raw, out var value) && value > 0 ? value : null;
    }

    private static string? ReadOptionalString(string environmentKey, string configurationKey)
    {
        var raw = Configuration[environmentKey] ?? Configuration[configurationKey];
        return string.IsNullOrWhiteSpace(raw) ? null : raw;
    }
}
