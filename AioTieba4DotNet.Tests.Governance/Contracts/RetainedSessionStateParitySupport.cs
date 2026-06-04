#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SessionAccount = AioTieba4DotNet.Session.Account;
using TiebaOptions = AioTieba4DotNet.Contracts.TiebaOptions;
using TiebaTransportMode = AioTieba4DotNet.Contracts.TiebaTransportMode;

namespace AioTieba4DotNet.Tests.Governance.Contracts;

internal static class RetainedSessionStateParitySupport
{
    internal static async Task<IReadOnlyList<SessionStateParityRow>> CaptureCanonicalRowsAsync()
    {
        return
        [
            await CaptureTbsLoadSuccessRowAsync(),
            await CaptureTbsRefreshSuccessRowAsync(),
            await CaptureZIdInitializationSuccessRowAsync(),
            await CaptureClientSyncSuccessRowAsync(),
            await CaptureWebSocketWarmupSuccessRowAsync(),
            await CaptureFallbackMutationRowAsync(),
            await CaptureTbsRefreshRollbackRowAsync(),
            await CaptureZIdRollbackRowAsync(),
            await CaptureClientSyncRollbackRowAsync(),
            await CaptureWebSocketWarmupRollbackRowAsync()
        ];
    }

    internal static SessionStateComparison Compare(SessionStateObservation expected, SessionStateObservation actual)
    {
        var diffs = new List<SessionStateDiff>();
        CompareStateSnapshot(diffs, "beforeState", expected.BeforeState, actual.BeforeState);
        CompareAccountSnapshot(diffs, "beforeAccount", expected.BeforeAccount, actual.BeforeAccount);
        CompareTimeline(diffs, expected.Timeline, actual.Timeline);
        CompareSequence(diffs, "mutationOrder", expected.MutationOrder, actual.MutationOrder);
        CompareStateSnapshot(diffs, "afterState", expected.AfterState, actual.AfterState);
        CompareAccountSnapshot(diffs, "afterAccount", expected.AfterAccount, actual.AfterAccount);
        CompareScalar(diffs, "rollbackRestoredPriorState", expected.RollbackRestoredPriorState, actual.RollbackRestoredPriorState);
        CompareScalar(diffs, "rollbackRestoredPriorAccount", expected.RollbackRestoredPriorAccount, actual.RollbackRestoredPriorAccount);
        CompareScalar(diffs, "outcome", expected.Outcome, actual.Outcome);
        CompareScalar(diffs, "resultSummary", expected.ResultSummary, actual.ResultSummary);
        CompareScalar(diffs, "exceptionType", expected.ExceptionType, actual.ExceptionType);
        CompareScalar(diffs, "exceptionMessage", expected.ExceptionMessage, actual.ExceptionMessage);
        CompareSequence(diffs, "transportPath", expected.TransportPath, actual.TransportPath);

        return new SessionStateComparison(diffs.Count == 0, diffs, expected, actual);
    }

    internal static SessionStateObservation CreateObservation(
        SessionStateSnapshot beforeState,
        SessionAccountSnapshot beforeAccount,
        IReadOnlyList<SessionTimelineEntry> timeline,
        SessionStateSnapshot afterState,
        SessionAccountSnapshot afterAccount,
        string outcome,
        string resultSummary,
        string exceptionType,
        string exceptionMessage,
        IReadOnlyList<string> transportPath)
    {
        return new SessionStateObservation(
            beforeState,
            beforeAccount,
            timeline,
            DeriveMutationOrder(beforeState, beforeAccount, timeline, afterState, afterAccount),
            afterState,
            afterAccount,
            Equals(beforeState, afterState),
            Equals(beforeAccount, afterAccount),
            outcome,
            resultSummary,
            exceptionType,
            exceptionMessage,
            transportPath);
    }

    internal static SessionStateSnapshot CaptureState(TiebaClientSession session)
    {
        var state = session.CurrentState;
        return new SessionStateSnapshot(
            state.Kind.ToString(),
            state.TbsState.ToString(),
            state.Tbs,
            state.ClientState.ToString(),
            state.ClientId,
            state.SampleId,
            state.ZIdState.ToString(),
            state.ZId,
            state.WebSocketState.ToString());
    }

    internal static SessionAccountSnapshot CaptureAccount(TiebaClientSession session)
    {
        var account = session.HttpCore.Account;
        return new SessionAccountSnapshot(account?.Tbs, account?.ClientId, account?.SampleId, account?.ZId);
    }

    private static async Task<SessionStateParityRow> CaptureTbsLoadSuccessRowAsync()
    {
        RetainedSessionStateRecorder? recorder = null;
        using var session = CreateAuthenticatedSession(
            loadTbsAsync: _ =>
            {
                recorder!.Record("loader.observed");
                return Task.FromResult("loaded-tbs");
            });

        recorder = new RetainedSessionStateRecorder(session);
        var result = await session.GetTbsAsync("RetainedLoadTbs");
        var actual = recorder.CompleteSuccess(result);
        var expected = CreateObservation(
            AuthenticatedState(),
            AccountSnapshot(),
            [new SessionTimelineEntry("loader.observed", AuthenticatedState(tbsState: TiebaSessionResourceState.Initializing), AccountSnapshot())],
            AuthenticatedState(tbsState: TiebaSessionResourceState.Ready, tbs: "loaded-tbs"),
            AccountSnapshot(tbs: "loaded-tbs"),
            "success",
            "loaded-tbs",
            string.Empty,
            string.Empty,
            []);

        return CreateRow(
            auditUnit: "session.tbs.load",
            notes: "TBS load must move Pending -> Initializing -> Ready and only publish the loaded value after the loader completes.",
            expected: expected,
            actual: actual);
    }

    private static async Task<SessionStateParityRow> CaptureTbsRefreshSuccessRowAsync()
    {
        RetainedSessionStateRecorder? recorder = null;
        using var session = CreateAuthenticatedSession(
            loadTbsAsync: _ =>
            {
                recorder!.Record("loader.observed");
                return Task.FromResult("refreshed-tbs");
            });
        session.UpdateTbs("stable-tbs");

        recorder = new RetainedSessionStateRecorder(session);
        var result = await session.RefreshTbsAsync("RetainedRefreshTbs");
        var actual = recorder.CompleteSuccess(result);
        var expected = CreateObservation(
            AuthenticatedState(tbsState: TiebaSessionResourceState.Ready, tbs: "stable-tbs"),
            AccountSnapshot(tbs: "stable-tbs"),
            [new SessionTimelineEntry("loader.observed", AuthenticatedState(tbsState: TiebaSessionResourceState.Initializing, tbs: "stable-tbs"), AccountSnapshot(tbs: "stable-tbs"))],
            AuthenticatedState(tbsState: TiebaSessionResourceState.Ready, tbs: "refreshed-tbs"),
            AccountSnapshot(tbs: "refreshed-tbs"),
            "success",
            "refreshed-tbs",
            string.Empty,
            string.Empty,
            []);

        return CreateRow(
            auditUnit: "session.tbs.refresh",
            notes: "TBS refresh must keep the prior token visible while loading and replace it only after the refresh succeeds.",
            expected: expected,
            actual: actual);
    }

    private static async Task<SessionStateParityRow> CaptureZIdInitializationSuccessRowAsync()
    {
        using var session = CreateAuthenticatedSession();
        var recorder = new RetainedSessionStateRecorder(session);
        var result = await session.ExecuteZIdInitializationAsync(
            "RetainedInitZId",
            _ =>
            {
                recorder.Record("executor.observed");
                return Task.FromResult("zid-001");
            });
        recorder.Record("after-executor");
        session.UpdateZId(result);
        recorder.Record("after-apply");

        var actual = recorder.CompleteSuccess(result);
        var expected = CreateObservation(
            AuthenticatedState(),
            AccountSnapshot(),
            [
                new SessionTimelineEntry("executor.observed", AuthenticatedState(zIdState: TiebaSessionResourceState.Initializing), AccountSnapshot()),
                new SessionTimelineEntry("after-executor", AuthenticatedState(zIdState: TiebaSessionResourceState.Initializing), AccountSnapshot()),
                new SessionTimelineEntry("after-apply", AuthenticatedState(zIdState: TiebaSessionResourceState.Ready, zId: "zid-001"), AccountSnapshot(zId: "zid-001"))
            ],
            AuthenticatedState(zIdState: TiebaSessionResourceState.Ready, zId: "zid-001"),
            AccountSnapshot(zId: "zid-001"),
            "success",
            "zid-001",
            string.Empty,
            string.Empty,
            []);

        return CreateRow(
            auditUnit: "session.zid.init",
            notes: "ZId initialization must expose an Initializing phase, then publish the ready value only when the follow-up mutation applies.",
            expected: expected,
            actual: actual);
    }

    private static async Task<SessionStateParityRow> CaptureClientSyncSuccessRowAsync()
    {
        using var session = CreateAuthenticatedSession();
        var recorder = new RetainedSessionStateRecorder(session);
        var result = await session.ExecuteClientSyncAsync(
            "RetainedClientSync",
            _ =>
            {
                recorder.Record("executor.observed");
                return Task.FromResult(("client-001", "sample-001"));
            });
        recorder.Record("after-executor");
        session.UpdateClientIdentifiers(result.ClientId, result.SampleId);
        recorder.Record("after-apply");

        var resultSummary = FormatClientSyncResult(result.ClientId, result.SampleId);
        var actual = recorder.CompleteSuccess(resultSummary);
        var expected = CreateObservation(
            AuthenticatedState(),
            AccountSnapshot(),
            [
                new SessionTimelineEntry("executor.observed", AuthenticatedState(clientState: TiebaSessionResourceState.Initializing), AccountSnapshot()),
                new SessionTimelineEntry("after-executor", AuthenticatedState(clientState: TiebaSessionResourceState.Initializing), AccountSnapshot()),
                new SessionTimelineEntry("after-apply", AuthenticatedState(clientState: TiebaSessionResourceState.Ready, clientId: "client-001", sampleId: "sample-001"), AccountSnapshot(clientId: "client-001", sampleId: "sample-001"))
            ],
            AuthenticatedState(clientState: TiebaSessionResourceState.Ready, clientId: "client-001", sampleId: "sample-001"),
            AccountSnapshot(clientId: "client-001", sampleId: "sample-001"),
            "success",
            resultSummary,
            string.Empty,
            string.Empty,
            []);

        return CreateRow(
            auditUnit: "session.client-sync",
            notes: "ClientId and SampleId must stay coupled: the initializing phase happens once, and both identifiers publish together when the sync mutation applies.",
            expected: expected,
            actual: actual);
    }

    private static async Task<SessionStateParityRow> CaptureWebSocketWarmupSuccessRowAsync()
    {
        RetainedSessionStateRecorder? recorder = null;
        var wsCore = new SessionStateRecordingWsCore(
            onConnectAsync: _ =>
            {
                recorder!.Record("connect.observed");
                return Task.CompletedTask;
            });
        using var session = CreateAuthenticatedSession(wsCore: wsCore);

        recorder = new RetainedSessionStateRecorder(session);
        await session.WarmUpWebSocketAsync("RetainedWarmUpWebSocket");
        var actual = recorder.CompleteSuccess("connected");
        var expected = CreateObservation(
            AuthenticatedState(),
            AccountSnapshot(),
            [new SessionTimelineEntry("connect.observed", AuthenticatedState(webSocketState: TiebaSessionResourceState.Initializing), AccountSnapshot())],
            AuthenticatedState(webSocketState: TiebaSessionResourceState.Ready),
            AccountSnapshot(),
            "success",
            "connected",
            string.Empty,
            string.Empty,
            []);

        return CreateRow(
            auditUnit: "session.websocket.warmup",
            notes: "WebSocket warmup must expose Initializing before the connect completes and only transition to Ready after the connect succeeds.",
            expected: expected,
            actual: actual);
    }

    private static async Task<SessionStateParityRow> CaptureFallbackMutationRowAsync()
    {
        RetainedSessionStateRecorder? recorder = null;
        var transportPath = new List<string>();
        var wsCore = new SessionStateRecordingWsCore(
            onConnectAsync: _ =>
            {
                transportPath.Add("ws.connect");
                recorder!.Record("ws.connect.observed");
                return Task.FromException(new WebSocketException(WebSocketError.NotAWebSocket, "simulated websocket fallback failure"));
            });
        using var session = CreateAuthenticatedSession(wsCore: wsCore, transportMode: TiebaTransportMode.Auto);

        recorder = new RetainedSessionStateRecorder(session);
        var dispatcher = new TiebaOperationDispatcher(session);
        var result = await dispatcher.ExecuteAsync(
            new TiebaOperationDescriptor<(string ClientId, string SampleId)>(
                "RetainedFallbackMutation",
                TiebaOperationCapabilities.WebSocketPreferred(requiresAuthentication: true),
                ExecuteHttpAsync: (_, _) =>
                {
                    transportPath.Add("http.execute");
                    recorder.Record("http.execute.observed");
                    return Task.FromResult(("fallback-client-id", "fallback-sample-id"));
                },
                ExecuteWebSocketAsync: (_, _) => Task.FromResult(("unexpected-ws-client-id", "unexpected-ws-sample-id")),
                ApplySessionMutation: (currentSession, mutationResult) =>
                {
                    transportPath.Add("apply-mutation");
                    currentSession.UpdateClientIdentifiers(mutationResult.ClientId, mutationResult.SampleId);
                    recorder.Record("after-apply");
                }));

        var resultSummary = FormatClientSyncResult(result.ClientId, result.SampleId);
        var actual = recorder.CompleteSuccess(resultSummary, transportPath);
        var expected = CreateObservation(
            AuthenticatedState(),
            AccountSnapshot(),
            [
                new SessionTimelineEntry("ws.connect.observed", AuthenticatedState(webSocketState: TiebaSessionResourceState.Initializing), AccountSnapshot()),
                new SessionTimelineEntry("http.execute.observed", AuthenticatedState(), AccountSnapshot()),
                new SessionTimelineEntry("after-apply", AuthenticatedState(clientState: TiebaSessionResourceState.Ready, clientId: "fallback-client-id", sampleId: "fallback-sample-id"), AccountSnapshot(clientId: "fallback-client-id", sampleId: "fallback-sample-id"))
            ],
            AuthenticatedState(clientState: TiebaSessionResourceState.Ready, clientId: "fallback-client-id", sampleId: "fallback-sample-id"),
            AccountSnapshot(clientId: "fallback-client-id", sampleId: "fallback-sample-id"),
            "success",
            resultSummary,
            string.Empty,
            string.Empty,
            ["ws.connect", "http.execute", "apply-mutation"]);

        return CreateRow(
            auditUnit: "dispatcher.websocket-fallback.session-mutation",
            notes: "When websocket warmup fails, the dispatcher must restore websocket state first, then run the HTTP fallback, then apply the session mutation from the fallback result.",
            expected: expected,
            actual: actual);
    }

    private static async Task<SessionStateParityRow> CaptureTbsRefreshRollbackRowAsync()
    {
        RetainedSessionStateRecorder? recorder = null;
        using var session = CreateAuthenticatedSession(
            loadTbsAsync: _ =>
            {
                recorder!.Record("loader.observed");
                return Task.FromException<string>(new InvalidOperationException("simulated tbs refresh failure"));
            });
        session.UpdateTbs("stable-tbs");

        recorder = new RetainedSessionStateRecorder(session);
        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await session.RefreshTbsAsync("RetainedRefreshTbsRollback"));
        var actual = recorder.CompleteFailure(exception);
        var expected = CreateObservation(
            AuthenticatedState(tbsState: TiebaSessionResourceState.Ready, tbs: "stable-tbs"),
            AccountSnapshot(tbs: "stable-tbs"),
            [new SessionTimelineEntry("loader.observed", AuthenticatedState(tbsState: TiebaSessionResourceState.Initializing, tbs: "stable-tbs"), AccountSnapshot(tbs: "stable-tbs"))],
            AuthenticatedState(tbsState: TiebaSessionResourceState.Ready, tbs: "stable-tbs"),
            AccountSnapshot(tbs: "stable-tbs"),
            "failure",
            string.Empty,
            nameof(InvalidOperationException),
            "simulated tbs refresh failure",
            []);

        return CreateRow(
            auditUnit: "session.tbs.refresh.rollback",
            notes: "Failed TBS refresh must restore the prior state and account token exactly instead of leaving an initializing or partially replaced value behind.",
            expected: expected,
            actual: actual);
    }

    private static async Task<SessionStateParityRow> CaptureZIdRollbackRowAsync()
    {
        using var session = CreateAuthenticatedSession();
        session.UpdateZId("stable-zid");
        var recorder = new RetainedSessionStateRecorder(session);
        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await session.ExecuteZIdInitializationAsync(
                "RetainedInitZIdRollback",
                _ =>
                {
                    recorder.Record("executor.observed");
                    return Task.FromException<string>(new InvalidOperationException("simulated z-id init failure"));
                }));
        var actual = recorder.CompleteFailure(exception);
        var expected = CreateObservation(
            AuthenticatedState(zIdState: TiebaSessionResourceState.Ready, zId: "stable-zid"),
            AccountSnapshot(zId: "stable-zid"),
            [new SessionTimelineEntry("executor.observed", AuthenticatedState(zIdState: TiebaSessionResourceState.Initializing, zId: "stable-zid"), AccountSnapshot(zId: "stable-zid"))],
            AuthenticatedState(zIdState: TiebaSessionResourceState.Ready, zId: "stable-zid"),
            AccountSnapshot(zId: "stable-zid"),
            "failure",
            string.Empty,
            nameof(InvalidOperationException),
            "simulated z-id init failure",
            []);

        return CreateRow(
            auditUnit: "session.zid.init.rollback",
            notes: "Failed ZId initialization must restore the previous ready state and value instead of leaving the session stuck in Initializing or with a partial mutation.",
            expected: expected,
            actual: actual);
    }

    private static async Task<SessionStateParityRow> CaptureClientSyncRollbackRowAsync()
    {
        using var session = CreateAuthenticatedSession();
        session.UpdateClientIdentifiers("stable-client-id", "stable-sample-id");
        var recorder = new RetainedSessionStateRecorder(session);
        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await session.ExecuteClientSyncAsync(
                "RetainedClientSyncRollback",
                _ =>
                {
                    recorder.Record("executor.observed");
                    return Task.FromException<(string ClientId, string SampleId)>(new InvalidOperationException("simulated client sync failure"));
                }));
        var actual = recorder.CompleteFailure(exception);
        var expected = CreateObservation(
            AuthenticatedState(clientState: TiebaSessionResourceState.Ready, clientId: "stable-client-id", sampleId: "stable-sample-id"),
            AccountSnapshot(clientId: "stable-client-id", sampleId: "stable-sample-id"),
            [new SessionTimelineEntry("executor.observed", AuthenticatedState(clientState: TiebaSessionResourceState.Initializing, clientId: "stable-client-id", sampleId: "stable-sample-id"), AccountSnapshot(clientId: "stable-client-id", sampleId: "stable-sample-id"))],
            AuthenticatedState(clientState: TiebaSessionResourceState.Ready, clientId: "stable-client-id", sampleId: "stable-sample-id"),
            AccountSnapshot(clientId: "stable-client-id", sampleId: "stable-sample-id"),
            "failure",
            string.Empty,
            nameof(InvalidOperationException),
            "simulated client sync failure",
            []);

        return CreateRow(
            auditUnit: "session.client-sync.rollback",
            notes: "Failed client sync must restore the previous ClientId and SampleId pair exactly instead of leaving either field partially changed.",
            expected: expected,
            actual: actual);
    }

    private static async Task<SessionStateParityRow> CaptureWebSocketWarmupRollbackRowAsync()
    {
        RetainedSessionStateRecorder? recorder = null;
        var wsCore = new SessionStateRecordingWsCore(
            onConnectAsync: _ =>
            {
                recorder!.Record("connect.observed");
                return Task.FromException(new InvalidOperationException("simulated websocket warmup failure"));
            });
        using var session = CreateAuthenticatedSession(wsCore: wsCore);

        recorder = new RetainedSessionStateRecorder(session);
        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await session.WarmUpWebSocketAsync("RetainedWarmUpWebSocketRollback"));
        var actual = recorder.CompleteFailure(exception);
        var expected = CreateObservation(
            AuthenticatedState(),
            AccountSnapshot(),
            [new SessionTimelineEntry("connect.observed", AuthenticatedState(webSocketState: TiebaSessionResourceState.Initializing), AccountSnapshot())],
            AuthenticatedState(),
            AccountSnapshot(),
            "failure",
            string.Empty,
            nameof(InvalidOperationException),
            "simulated websocket warmup failure",
            []);

        return CreateRow(
            auditUnit: "session.websocket.warmup.rollback",
            notes: "Failed websocket warmup must restore the prior websocket state exactly instead of leaving the session in Initializing.",
            expected: expected,
            actual: actual);
    }

    private static TiebaClientSession CreateAuthenticatedSession(
        Func<CancellationToken, Task<string>>? loadTbsAsync = null,
        ITiebaWsCore? wsCore = null,
        TiebaTransportMode transportMode = TiebaTransportMode.Auto)
    {
        var options = new TiebaOptions
        {
            Bduss = new string('b', 192),
            Stoken = new string('s', 64),
            TransportMode = transportMode
        };
        return new TiebaClientSession(options, new SessionStateNoOpHttpCore(), wsCore, loadTbsAsync);
    }

    private static SessionStateParityRow CreateRow(string auditUnit, string notes, SessionStateObservation expected, SessionStateObservation actual)
    {
        return new SessionStateParityRow(auditUnit, notes, Compare(expected, actual));
    }

    private static SessionStateSnapshot AuthenticatedState(
        TiebaSessionResourceState tbsState = TiebaSessionResourceState.Pending,
        string? tbs = null,
        TiebaSessionResourceState clientState = TiebaSessionResourceState.Pending,
        string? clientId = null,
        string? sampleId = null,
        TiebaSessionResourceState zIdState = TiebaSessionResourceState.Pending,
        string? zId = null,
        TiebaSessionResourceState webSocketState = TiebaSessionResourceState.Pending)
    {
        return new SessionStateSnapshot(
            TiebaSessionKind.Authenticated.ToString(),
            tbsState.ToString(),
            tbs,
            clientState.ToString(),
            clientId,
            sampleId,
            zIdState.ToString(),
            zId,
            webSocketState.ToString());
    }

    private static SessionAccountSnapshot AccountSnapshot(string? tbs = null, string? clientId = null, string? sampleId = null, string? zId = null)
    {
        return new SessionAccountSnapshot(tbs, clientId, sampleId, zId);
    }

    private static string FormatClientSyncResult(string clientId, string sampleId)
    {
        return $"clientId={clientId};sampleId={sampleId}";
    }

    private static string[] DeriveMutationOrder(
        SessionStateSnapshot beforeState,
        SessionAccountSnapshot beforeAccount,
        IReadOnlyList<SessionTimelineEntry> timeline,
        SessionStateSnapshot afterState,
        SessionAccountSnapshot afterAccount)
    {
        var order = new List<string>();
        var currentState = beforeState;
        var currentAccount = beforeAccount;

        foreach (var entry in timeline)
        {
            AppendChanges(order, entry.Step, currentState, entry.State, currentAccount, entry.Account);
            currentState = entry.State;
            currentAccount = entry.Account;
        }

        AppendChanges(order, "after", currentState, afterState, currentAccount, afterAccount);
        return [.. order];
    }

    private static void AppendChanges(
        List<string> order,
        string step,
        SessionStateSnapshot beforeState,
        SessionStateSnapshot afterState,
        SessionAccountSnapshot beforeAccount,
        SessionAccountSnapshot afterAccount)
    {
        AppendChange(order, step, "state.kind", beforeState.Kind, afterState.Kind);
        AppendChange(order, step, "state.tbsState", beforeState.TbsState, afterState.TbsState);
        AppendChange(order, step, "state.tbs", beforeState.Tbs, afterState.Tbs);
        AppendChange(order, step, "state.clientState", beforeState.ClientState, afterState.ClientState);
        AppendChange(order, step, "state.clientId", beforeState.ClientId, afterState.ClientId);
        AppendChange(order, step, "state.sampleId", beforeState.SampleId, afterState.SampleId);
        AppendChange(order, step, "state.zIdState", beforeState.ZIdState, afterState.ZIdState);
        AppendChange(order, step, "state.zId", beforeState.ZId, afterState.ZId);
        AppendChange(order, step, "state.webSocketState", beforeState.WebSocketState, afterState.WebSocketState);
        AppendChange(order, step, "account.tbs", beforeAccount.Tbs, afterAccount.Tbs);
        AppendChange(order, step, "account.clientId", beforeAccount.ClientId, afterAccount.ClientId);
        AppendChange(order, step, "account.sampleId", beforeAccount.SampleId, afterAccount.SampleId);
        AppendChange(order, step, "account.zId", beforeAccount.ZId, afterAccount.ZId);
    }

    private static void AppendChange(List<string> order, string step, string path, string? before, string? after)
    {
        if (string.Equals(before, after, StringComparison.Ordinal))
            return;

        order.Add($"{step}: {path} {FormatNullable(before)} -> {FormatNullable(after)}");
    }

    private static string FormatNullable(string? value) => value ?? "<null>";

    private static void CompareTimeline(List<SessionStateDiff> diffs, IReadOnlyList<SessionTimelineEntry> expected, IReadOnlyList<SessionTimelineEntry> actual)
    {
        if (expected.Count != actual.Count)
            diffs.Add(new SessionStateDiff("timeline.length", expected.Count.ToString(), actual.Count.ToString()));

        foreach (var index in Enumerable.Range(0, Math.Min(expected.Count, actual.Count)))
        {
            var expectedEntry = expected[index];
            var actualEntry = actual[index];
            CompareScalar(diffs, $"timeline[{index}].step", expectedEntry.Step, actualEntry.Step);
            CompareStateSnapshot(diffs, $"timeline[{index}].state", expectedEntry.State, actualEntry.State);
            CompareAccountSnapshot(diffs, $"timeline[{index}].account", expectedEntry.Account, actualEntry.Account);
        }
    }

    private static void CompareStateSnapshot(List<SessionStateDiff> diffs, string path, SessionStateSnapshot expected, SessionStateSnapshot actual)
    {
        CompareScalar(diffs, $"{path}.kind", expected.Kind, actual.Kind);
        CompareScalar(diffs, $"{path}.tbsState", expected.TbsState, actual.TbsState);
        CompareScalar(diffs, $"{path}.tbs", expected.Tbs, actual.Tbs);
        CompareScalar(diffs, $"{path}.clientState", expected.ClientState, actual.ClientState);
        CompareScalar(diffs, $"{path}.clientId", expected.ClientId, actual.ClientId);
        CompareScalar(diffs, $"{path}.sampleId", expected.SampleId, actual.SampleId);
        CompareScalar(diffs, $"{path}.zIdState", expected.ZIdState, actual.ZIdState);
        CompareScalar(diffs, $"{path}.zId", expected.ZId, actual.ZId);
        CompareScalar(diffs, $"{path}.webSocketState", expected.WebSocketState, actual.WebSocketState);
    }

    private static void CompareAccountSnapshot(List<SessionStateDiff> diffs, string path, SessionAccountSnapshot expected, SessionAccountSnapshot actual)
    {
        CompareScalar(diffs, $"{path}.tbs", expected.Tbs, actual.Tbs);
        CompareScalar(diffs, $"{path}.clientId", expected.ClientId, actual.ClientId);
        CompareScalar(diffs, $"{path}.sampleId", expected.SampleId, actual.SampleId);
        CompareScalar(diffs, $"{path}.zId", expected.ZId, actual.ZId);
    }

    private static void CompareScalar(List<SessionStateDiff> diffs, string path, string? expected, string? actual)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
            diffs.Add(new SessionStateDiff(path, FormatNullable(expected), FormatNullable(actual)));
    }

    private static void CompareScalar(List<SessionStateDiff> diffs, string path, bool expected, bool actual)
    {
        if (expected != actual)
            diffs.Add(new SessionStateDiff(path, expected.ToString(), actual.ToString()));
    }

    private static void CompareSequence(List<SessionStateDiff> diffs, string path, IReadOnlyList<string> expected, IReadOnlyList<string> actual)
    {
        if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
            diffs.Add(new SessionStateDiff(path, string.Join(" | ", expected), string.Join(" | ", actual)));
    }
}

internal sealed class RetainedSessionStateRecorder(TiebaClientSession session)
{
    private readonly SessionAccountSnapshot _beforeAccount = RetainedSessionStateParitySupport.CaptureAccount(session);
    private readonly SessionStateSnapshot _beforeState = RetainedSessionStateParitySupport.CaptureState(session);
    private readonly List<SessionTimelineEntry> _timeline = [];

    internal void Record(string step)
    {
        _timeline.Add(new SessionTimelineEntry(step, RetainedSessionStateParitySupport.CaptureState(session), RetainedSessionStateParitySupport.CaptureAccount(session)));
    }

    internal SessionStateObservation CompleteSuccess(string resultSummary, IReadOnlyList<string>? transportPath = null)
    {
        return RetainedSessionStateParitySupport.CreateObservation(
            _beforeState,
            _beforeAccount,
            [.. _timeline],
            RetainedSessionStateParitySupport.CaptureState(session),
            RetainedSessionStateParitySupport.CaptureAccount(session),
            "success",
            resultSummary,
            string.Empty,
            string.Empty,
            transportPath ?? []);
    }

    internal SessionStateObservation CompleteFailure(Exception exception, IReadOnlyList<string>? transportPath = null)
    {
        return RetainedSessionStateParitySupport.CreateObservation(
            _beforeState,
            _beforeAccount,
            [.. _timeline],
            RetainedSessionStateParitySupport.CaptureState(session),
            RetainedSessionStateParitySupport.CaptureAccount(session),
            "failure",
            string.Empty,
            exception.GetType().Name,
            exception.Message,
            transportPath ?? []);
    }
}

internal sealed class SessionStateRecordingWsCore(Func<CancellationToken, Task>? onConnectAsync = null) : ITiebaWsCore
{
    public SessionAccount? Account { get; private set; }

    public void SetAccount(SessionAccount newAccount)
    {
        Account = newAccount;
    }

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        return onConnectAsync?.Invoke(cancellationToken) ?? Task.CompletedTask;
    }

    public Task SendAsync(WSReq req, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("The retained session parity support does not send raw websocket frames.");
    }

    public Task<WSRes> SendAsync(int cmd, byte[] data, bool encrypt = true, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("The retained session parity support does not use websocket request/response sends.");
    }

    public async IAsyncEnumerable<WSRes> ListenAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        yield break;
    }

    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

internal sealed class SessionStateNoOpHttpCore : ITiebaHttpCore, IDisposable
{
    public SessionAccount? Account { get; private set; }

    public HttpClient HttpClient { get; } = new();

    public void Dispose()
    {
        HttpClient.Dispose();
    }

    public void SetAccount(SessionAccount newAccount)
    {
        Account = newAccount;
    }

    public Task<string> SendAsync(Func<HttpRequestMessage> requestFactory, bool allowRetry = false, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("The retained session parity support does not use ITiebaHttpCore.SendAsync.");
    }

    public Task<string> SendAppFormAsync(Uri uri, List<KeyValuePair<string, string>> data, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("The retained session parity support does not use ITiebaHttpCore.SendAppFormAsync.");
    }

    public Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("The retained session parity support does not use ITiebaHttpCore.SendAppProtoAsync.");
    }

    public Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("The retained session parity support does not use ITiebaHttpCore.SendWebGetAsync.");
    }

    public Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("The retained session parity support does not use ITiebaHttpCore.SendWebFormAsync.");
    }
}

internal sealed record SessionStateParityRow(string AuditUnit, string Notes, SessionStateComparison Comparison);

internal sealed record SessionStateComparison(
    bool Match,
    IReadOnlyList<SessionStateDiff> Diffs,
    SessionStateObservation Expected,
    SessionStateObservation Actual)
{
    internal string ToFailureMessage(string auditUnit)
    {
        var diffText = Diffs.Count == 0
            ? "<none>"
            : string.Join(Environment.NewLine, Diffs.Select(static diff => $"- {diff.Path}: expected '{diff.Expected}' actual '{diff.Actual}'"));
        return $"Session state parity diff for '{auditUnit}' must stay deterministic.{Environment.NewLine}{diffText}";
    }
}

internal sealed record SessionStateObservation(
    SessionStateSnapshot BeforeState,
    SessionAccountSnapshot BeforeAccount,
    IReadOnlyList<SessionTimelineEntry> Timeline,
    IReadOnlyList<string> MutationOrder,
    SessionStateSnapshot AfterState,
    SessionAccountSnapshot AfterAccount,
    bool RollbackRestoredPriorState,
    bool RollbackRestoredPriorAccount,
    string Outcome,
    string ResultSummary,
    string ExceptionType,
    string ExceptionMessage,
    IReadOnlyList<string> TransportPath);

internal sealed record SessionTimelineEntry(string Step, SessionStateSnapshot State, SessionAccountSnapshot Account);

internal sealed record SessionStateSnapshot(
    string Kind,
    string TbsState,
    string? Tbs,
    string ClientState,
    string? ClientId,
    string? SampleId,
    string ZIdState,
    string? ZId,
    string WebSocketState);

internal sealed record SessionAccountSnapshot(string? Tbs, string? ClientId, string? SampleId, string? ZId);

internal sealed record SessionStateDiff(string Path, string Expected, string Actual);
