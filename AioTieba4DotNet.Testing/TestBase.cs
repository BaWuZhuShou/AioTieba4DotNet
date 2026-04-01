#nullable enable
using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Testing;
using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Transport.Http;
using AioTieba4DotNet.Transport.WebSockets;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests;

public abstract class TestBase : IAsyncDisposable
{
    private bool _disposed;

    protected TestBase()
    {
        Environment = TiebaTestEnvironment.Current;
        var account = new AioTieba4DotNet.Session.Account(Environment.Bduss, Environment.Stoken);
        HttpCore httpCore = new();
        httpCore.SetAccount(account);
        WebsocketCore websocketCore = new(account);

        HttpCore = httpCore;
        WebsocketCore = websocketCore;
        Client = new TiebaClient(new TiebaOptions
        {
            Bduss = string.IsNullOrWhiteSpace(Environment.Bduss) ? null : Environment.Bduss,
            Stoken = string.IsNullOrWhiteSpace(Environment.Stoken) ? null : Environment.Stoken
        });
        Cleanup = new TestCleanupOrchestrator();
    }

    protected TiebaTestEnvironment Environment { get; }

    protected TestCleanupOrchestrator Cleanup { get; }

    protected string Bduss => Environment.Bduss;

    protected string Stoken => Environment.Stoken;

    protected string ConfiguredSafeForumQuery => Environment.ConfiguredSafeForumQuery;

    protected string ConfiguredCanonicalSafeForumName => Environment.ConfiguredCanonicalSafeForumName;

    protected long? OwnedThreadId => Environment.OwnedThreadId;

    protected long? OwnedReplyId => Environment.OwnedReplyId;

    protected string? SafeTargetPortrait => Environment.SafeTargetPortrait;

    protected string? SafeTargetUserName => Environment.SafeTargetUserName;

    protected long? SafeTargetUserId => Environment.SafeTargetUserId;

    protected string? SafeMessageRecipient => Environment.SafeMessageRecipient;

    protected long? SafeChatroomId => Environment.SafeChatroomId;

    protected bool EnableAdminMutationTests => Environment.EnableAdminMutationTests;

    internal ITiebaHttpCore HttpCore { get; }

    internal ITiebaWsCore WebsocketCore { get; }

    protected TiebaClient Client { get; }

    protected bool IsAuthenticated => Environment.HasCredentials;

    protected TiebaClient CreateClient(global::AioTieba4DotNet.Contracts.TiebaTransportMode transportMode)
    {
        return new TiebaClient(new TiebaOptions
        {
            Bduss = string.IsNullOrWhiteSpace(Environment.Bduss) ? null : Environment.Bduss,
            Stoken = string.IsNullOrWhiteSpace(Environment.Stoken) ? null : Environment.Stoken,
            TransportMode = transportMode
        });
    }

    protected void EnsureAuthenticated()
    {
        TestFixtureGates.EnsureAuthenticated(Environment);
    }

    protected long RequireOwnedThreadFixture(string operationName)
    {
        return TestFixtureGates.RequirePositiveLong(Environment, OwnedThreadId, operationName,
            "owned thread fixture", "TIEBA_OWNEDTHREADID or TieBa:OwnedThreadId");
    }

    protected long RequireOwnedReplyFixture(string operationName)
    {
        return TestFixtureGates.RequirePositiveLong(Environment, OwnedReplyId, operationName,
            "owned reply fixture", "TIEBA_OWNEDREPLYID or TieBa:OwnedReplyId");
    }

    protected string RequireSafeTargetPortraitFixture(string operationName)
    {
        return TestFixtureGates.RequireNonEmptyString(Environment, SafeTargetPortrait, operationName,
            "safe target portrait", "TIEBA_SAFETARGETPORTRAIT or TieBa:SafeTargetPortrait");
    }

    protected string RequireSafeTargetUserNameFixture(string operationName)
    {
        return TestFixtureGates.RequireNonEmptyString(Environment, SafeTargetUserName, operationName,
            "safe target user name", "TIEBA_SAFETARGETUSERNAME or TieBa:SafeTargetUserName");
    }

    protected string RequireSafeForumQueryFixture(string operationName)
    {
        return TestFixtureGates.RequireNonEmptyString(Environment, ConfiguredSafeForumQuery, operationName,
            "safe forum query", "TIEBA_SAFEFORUMQUERY or TieBa:SafeForumQuery");
    }

    protected SafeForumFixture RequireConfiguredSafeForumReadFixture(string operationName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        if (string.IsNullOrWhiteSpace(ConfiguredSafeForumQuery))
        {
            Assert.Inconclusive(
                $"Skipping {operationName}: no safe forum query is configured. Set TIEBA_SAFEFORUMQUERY or TieBa:SafeForumQuery.");
        }

        var resolvedName = string.IsNullOrWhiteSpace(ConfiguredCanonicalSafeForumName)
            ? ConfiguredSafeForumQuery
            : ConfiguredCanonicalSafeForumName;

        return new SafeForumFixture(ConfiguredSafeForumQuery, resolvedName, 0, resolvedName);
    }

    protected async Task<SafeForumFixture> RequireSafeForumFixtureAsync(string operationName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        EnsureAuthenticated();
        var query = RequireSafeForumQueryFixture(operationName);
        ulong fid;
        string resolvedName;

        try
        {
            fid = await Client.Forums.GetFidAsync(query, cancellationToken);
            resolvedName = await Client.Forums.GetFnameAsync(fid, cancellationToken);
        }
        catch (TiebaException exception)
        {
            Assert.Inconclusive(
                $"Skipping {operationName}: unable to resolve the configured safe forum query '{query}' in this environment. {exception.Message}");
            return default;
        }

        if (fid == 0 || string.IsNullOrWhiteSpace(resolvedName))
            Assert.Inconclusive(
                $"Skipping {operationName}: unable to resolve the configured safe forum query '{query}'.");

        var canonicalName = string.IsNullOrWhiteSpace(ConfiguredCanonicalSafeForumName)
            ? resolvedName
            : ConfiguredCanonicalSafeForumName;

        return new SafeForumFixture(query, canonicalName, fid, resolvedName);
    }

    protected long RequireSafeTargetUserIdFixture(string operationName)
    {
        return TestFixtureGates.RequirePositiveLong(Environment, SafeTargetUserId, operationName,
            "safe target user id", "TIEBA_SAFETARGETUSERID or TieBa:SafeTargetUserId");
    }

    protected string RequireSafeMessageRecipientFixture(string operationName)
    {
        return TestFixtureGates.RequireNonEmptyString(Environment, SafeMessageRecipient, operationName,
            "safe message recipient", "TIEBA_SAFEMESSAGERECIPIENT or TieBa:SafeMessageRecipient");
    }

    protected long RequireSafeChatroomIdFixture(string operationName)
    {
        return TestFixtureGates.RequirePositiveLong(Environment, SafeChatroomId, operationName,
            "safe chatroom id", "TIEBA_SAFECHATROOMID or TieBa:SafeChatroomId");
    }

    protected void EnsureAdminMutationManualGate(string operationName)
    {
        TestFixtureGates.EnsureAdminMutationManualGate(Environment, operationName);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        Exception? cleanupException = null;

        try
        {
            await Cleanup.DisposeAsync();
        }
        catch (Exception exception)
        {
            cleanupException = exception;
        }
        finally
        {
            var cleanupReport = Cleanup.GetLastExecutionReport();
            if (cleanupReport is not null)
            {
                foreach (var line in cleanupReport.ToDisplayLines())
                    Console.WriteLine(line);
            }

            Client.Dispose();
            (WebsocketCore as IDisposable)?.Dispose();
            (HttpCore as IDisposable)?.Dispose();
        }

        _disposed = true;
        GC.SuppressFinalize(this);

        if (cleanupException is not null)
            ExceptionDispatchInfo.Capture(cleanupException).Throw();
    }
}
