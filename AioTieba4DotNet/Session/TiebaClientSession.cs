using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Transport.Http;
using AioTieba4DotNet.Transport.WebSockets;
using AioTieba4DotNet.Api.Login;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;

namespace AioTieba4DotNet.Session;

internal sealed class TiebaClientSession : IDisposable
{
    private readonly Account? _account;
    private readonly TiebaSessionStateStore _stateStore;
    private readonly TiebaSessionTbsService _tbsService;
    private readonly SemaphoreSlim _webSocketWarmupLock = new(1, 1);
    private readonly SemaphoreSlim _zIdLock = new(1, 1);
    private readonly SemaphoreSlim _clientSyncLock = new(1, 1);

    internal TiebaClientSession(TiebaOptions options, HttpClient? httpClient = null)
        : this(options, new HttpCore(options, httpClient))
    {
    }

    internal TiebaClientSession(TiebaOptions options, ITiebaHttpCore httpCore,
        ITiebaWsCore? wsCore = null,
        Func<CancellationToken, Task<string>>? loadTbsAsync = null)
    {
        TiebaOptionsValidator.Validate(options);
        Options = options;
        HttpCore = httpCore;
        _account = CreateAccount(options);
        _stateStore = new TiebaSessionStateStore(_account);

        if (_account != null) HttpCore.SetAccount(_account);

        WsCore = wsCore ?? new WebsocketCore(_account);
        if (_account != null) WsCore.SetAccount(_account);

        _tbsService = new TiebaSessionTbsService(RequireAuthenticatedAccount, _stateStore,
            loadTbsAsync ?? LoadTbsFromLoginAsync);
    }

    internal TiebaSessionState CurrentState => _stateStore.CurrentState;

    internal bool IsAuthenticated => CurrentState.IsAuthenticated;

    internal Account RequireAuthenticatedAccount(string operationName)
    {
        if (_account == null || string.IsNullOrWhiteSpace(_account.Bduss))
            throw TiebaSessionAuthPolicy.CreateMissingCredentialsException(operationName);

        return _account;
    }

    internal TiebaOptions Options { get; }

    internal ITiebaHttpCore HttpCore { get; }

    internal ITiebaWsCore WsCore { get; }

    internal Task<string> GetTbsAsync(CancellationToken cancellationToken = default) =>
        GetTbsAsync(nameof(GetTbsAsync), cancellationToken);

    internal Task<string> GetTbsAsync(string operationName, CancellationToken cancellationToken = default) =>
        _tbsService.GetAsync(operationName, cancellationToken);

    internal Task<string> RefreshTbsAsync(string operationName, CancellationToken cancellationToken = default) =>
        _tbsService.RefreshAsync(operationName, cancellationToken);

    internal async Task<string> EnsureTbsAsync(string operationName, CancellationToken cancellationToken = default)
    {
        var tbs = await GetTbsAsync(operationName, cancellationToken);
        if (string.IsNullOrWhiteSpace(tbs))
            throw TiebaSessionAuthPolicy.CreateMissingSessionStateException(operationName, nameof(Account.Tbs));

        return tbs;
    }

    internal async Task WarmUpWebSocketAsync(string operationName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        cancellationToken.ThrowIfCancellationRequested();

        await _webSocketWarmupLock.WaitAsync(cancellationToken);
        var previousState = _stateStore.CaptureWebSocketState();
        _stateStore.SetWebSocketInitializing();
        try
        {
            await WsCore.ConnectAsync(cancellationToken);
            _stateStore.SetWebSocketReady();
        }
        catch
        {
            _stateStore.RestoreWebSocketState(previousState);
            throw;
        }
        finally
        {
            _webSocketWarmupLock.Release();
        }
    }

    internal async Task<string> ExecuteZIdInitializationAsync(string operationName,
        Func<CancellationToken, Task<string>> executor,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentNullException.ThrowIfNull(executor);
        cancellationToken.ThrowIfCancellationRequested();
        _ = RequireAuthenticatedAccount(operationName);

        await _zIdLock.WaitAsync(cancellationToken);
        var previousState = _stateStore.CaptureZId();
        _stateStore.SetZIdInitializing();
        try
        {
            var zId = await executor(cancellationToken);
            if (string.IsNullOrWhiteSpace(zId))
                throw TiebaSessionAuthPolicy.CreateMissingSessionStateException(operationName, nameof(Account.ZId));

            return zId;
        }
        catch
        {
            _stateStore.RestoreZId(previousState.State, previousState.Value);
            throw;
        }
        finally
        {
            _zIdLock.Release();
        }
    }

    internal async Task<(string ClientId, string SampleId)> ExecuteClientSyncAsync(string operationName,
        Func<CancellationToken, Task<(string ClientId, string SampleId)>> executor,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentNullException.ThrowIfNull(executor);
        cancellationToken.ThrowIfCancellationRequested();
        _ = RequireAuthenticatedAccount(operationName);

        await _clientSyncLock.WaitAsync(cancellationToken);
        var previousState = _stateStore.CaptureClientSync();
        _stateStore.SetClientSyncInitializing();
        try
        {
            var result = await executor(cancellationToken);
            if (string.IsNullOrWhiteSpace(result.ClientId))
                throw TiebaSessionAuthPolicy.CreateMissingSessionStateException(operationName, nameof(Account.ClientId));

            if (string.IsNullOrWhiteSpace(result.SampleId))
                throw TiebaSessionAuthPolicy.CreateMissingSessionStateException(operationName, nameof(Account.SampleId));

            return result;
        }
        catch
        {
            _stateStore.RestoreClientSync(previousState.State, previousState.ClientId, previousState.SampleId);
            throw;
        }
        finally
        {
            _clientSyncLock.Release();
        }
    }

    internal void UpdateTbs(string tbs)
    {
        var account = RequireAuthenticatedAccount(nameof(UpdateTbs));
        if (string.IsNullOrWhiteSpace(tbs))
            throw TiebaSessionAuthPolicy.CreateMissingSessionStateException(nameof(UpdateTbs), nameof(Account.Tbs));

        account.Tbs = tbs;
        _stateStore.SetTbs(tbs);
    }

    internal void UpdateClientIdentifiers(string clientId, string sampleId)
    {
        var account = RequireAuthenticatedAccount(nameof(UpdateClientIdentifiers));
        if (string.IsNullOrWhiteSpace(clientId))
            throw TiebaSessionAuthPolicy.CreateMissingSessionStateException(nameof(UpdateClientIdentifiers),
                nameof(Account.ClientId));

        if (string.IsNullOrWhiteSpace(sampleId))
            throw TiebaSessionAuthPolicy.CreateMissingSessionStateException(nameof(UpdateClientIdentifiers),
                nameof(Account.SampleId));

        account.ClientId = clientId;
        account.SampleId = sampleId;
        _stateStore.SetClientIdentifiers(clientId, sampleId);
    }

    internal void UpdateZId(string zId)
    {
        var account = RequireAuthenticatedAccount(nameof(UpdateZId));
        if (string.IsNullOrWhiteSpace(zId))
            throw TiebaSessionAuthPolicy.CreateMissingSessionStateException(nameof(UpdateZId), nameof(Account.ZId));

        account.ZId = zId;
        _stateStore.SetZId(zId);
    }

    public void Dispose()
    {
        _tbsService.Dispose();
        _webSocketWarmupLock.Dispose();
        _zIdLock.Dispose();
        _clientSyncLock.Dispose();
        if (HttpCore is IDisposable httpCoreDisposable) httpCoreDisposable.Dispose();
        if (WsCore is IDisposable disposable) disposable.Dispose();
    }

    private static Account? CreateAccount(TiebaOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Bduss)) return null;
        return new Account(options.Bduss, options.Stoken ?? string.Empty);
    }

    private async Task<string> LoadTbsFromLoginAsync(CancellationToken cancellationToken)
    {
        var loginApi = new Login(HttpCore);
        var (_, tbs) = await loginApi.RequestAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(tbs))
            throw new TiebaConfigurationException("TBS initialization returned an empty value.");

        return tbs;
    }
}
