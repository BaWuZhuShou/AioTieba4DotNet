#nullable enable
using System;
using Microsoft.Extensions.Configuration;

namespace AioTieba4DotNet.Testing;

public sealed class TiebaTestEnvironment
{
    private static readonly Lazy<TiebaTestEnvironment> CurrentFactory = new(Create);

    private TiebaTestEnvironment(IConfiguration configuration)
    {
        Configuration = configuration;
        Bduss = configuration["BDUSS"] ?? configuration["TieBa:BDUSS"] ?? string.Empty;
        Stoken = configuration["STOKEN"] ?? configuration["TieBa:STOKEN"] ?? string.Empty;
        ConfiguredSafeForumQuery = configuration["SAFEFORUMQUERY"] ?? configuration["TieBa:SafeForumQuery"] ?? "lol欧服吧";
        ConfiguredCanonicalSafeForumName = configuration["SAFEFORUMNAME"] ?? configuration["TieBa:SafeForumName"] ?? "lol欧服";
        OwnedThreadId = ReadOptionalLong(configuration, "OWNEDTHREADID", "TieBa:OwnedThreadId");
        OwnedReplyId = ReadOptionalLong(configuration, "OWNEDREPLYID", "TieBa:OwnedReplyId");
        SafeTargetPortrait = ReadOptionalString(configuration, "SAFETARGETPORTRAIT", "TieBa:SafeTargetPortrait");
        SafeTargetUserName = ReadOptionalString(configuration, "SAFETARGETUSERNAME", "TieBa:SafeTargetUserName");
        SafeTargetUserId = ReadOptionalLong(configuration, "SAFETARGETUSERID", "TieBa:SafeTargetUserId");
        SafeMessageRecipient = ReadOptionalString(configuration, "SAFEMESSAGERECIPIENT", "TieBa:SafeMessageRecipient");
        SafeChatroomId = ReadOptionalLong(configuration, "SAFECHATROOMID", "TieBa:SafeChatroomId");
        EnableAdminMutationTests = ReadOptionalBoolean(configuration, "ENABLEADMINMUTATIONTESTS",
            "TieBa:EnableAdminMutationTests");
    }

    public static TiebaTestEnvironment Current => CurrentFactory.Value;

    public IConfiguration Configuration { get; }

    public string Bduss { get; }

    public string Stoken { get; }

    public string ConfiguredSafeForumQuery { get; }

    public string ConfiguredCanonicalSafeForumName { get; }

    public long? OwnedThreadId { get; }

    public long? OwnedReplyId { get; }

    public string? SafeTargetPortrait { get; }

    public string? SafeTargetUserName { get; }

    public long? SafeTargetUserId { get; }

    public string? SafeMessageRecipient { get; }

    public long? SafeChatroomId { get; }

    public bool EnableAdminMutationTests { get; }

    public bool HasCredentials => !string.IsNullOrWhiteSpace(Bduss);

    private static TiebaTestEnvironment Create()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", true)
            // Tracked template only. Real credentials must come from environment overrides
            // or from the optional untracked appsettings.fixtures.json file.
            .AddJsonFile("appsettings.test.json", true)
            .AddJsonFile("appsettings.fixtures.json", true)
            .AddEnvironmentVariables("TIEBA_")
            .Build();

        return new TiebaTestEnvironment(configuration);
    }

    private static long? ReadOptionalLong(IConfiguration configuration, string environmentKey, string configurationKey)
    {
        var raw = configuration[environmentKey] ?? configuration[configurationKey];
        return long.TryParse(raw, out var value) && value > 0 ? value : null;
    }

    private static string? ReadOptionalString(IConfiguration configuration, string environmentKey, string configurationKey)
    {
        var raw = configuration[environmentKey] ?? configuration[configurationKey];
        return string.IsNullOrWhiteSpace(raw) ? null : raw;
    }

    private static bool ReadOptionalBoolean(IConfiguration configuration, string environmentKey, string configurationKey)
    {
        var raw = configuration[environmentKey] ?? configuration[configurationKey];
        return bool.TryParse(raw, out var value) && value;
    }
}
