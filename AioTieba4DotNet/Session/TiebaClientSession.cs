using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.Login;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using AioTieba4DotNet.Internal.Mapping;

namespace AioTieba4DotNet.Session;

    internal sealed class TiebaClientSession : IDisposable
{
    private readonly Account? _account;
    private readonly Func<CancellationToken, Task<string>> _loadTbsAsync;
    private readonly SemaphoreSlim _tbsLock = new(1, 1);

    internal TiebaClientSession(global::AioTieba4DotNet.TiebaOptions options, HttpClient? httpClient = null)
        : this(options, new HttpCore(options, httpClient))
    {
    }

    internal TiebaClientSession(global::AioTieba4DotNet.TiebaOptions options, ITiebaHttpCore httpCore,
        ITiebaWsCore? wsCore = null,
        Func<CancellationToken, Task<string>>? loadTbsAsync = null)
    {
        TiebaOptionsValidator.Validate(options);
        Options = options;
        HttpCore = httpCore;
        _account = CreateAccount(options);

        if (_account != null) HttpCore.SetAccount(_account);

        WsCore = wsCore ?? new WebsocketCore(_account);
        if (_account != null) WsCore.SetAccount(_account);

        _loadTbsAsync = loadTbsAsync ?? LoadTbsFromLoginAsync;
    }

    internal TiebaSessionState CurrentState => TiebaSessionState.FromAccount(_account);

    internal bool IsAuthenticated => CurrentState.IsAuthenticated;

    internal Account RequireAuthenticatedAccount(string operationName)
    {
        if (_account == null || string.IsNullOrWhiteSpace(_account.Bduss))
            throw TiebaSessionAuthPolicy.CreateMissingCredentialsException(operationName);

        return _account;
    }

    internal global::AioTieba4DotNet.TiebaOptions Options { get; }

    internal ITiebaHttpCore HttpCore { get; }

    internal ITiebaWsCore WsCore { get; }

    internal async Task<string> GetTbsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var account = RequireAuthenticatedAccount(nameof(GetTbsAsync));
        if (!string.IsNullOrWhiteSpace(account.Tbs)) return account.Tbs;

        await _tbsLock.WaitAsync(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.IsNullOrWhiteSpace(account.Tbs)) return account.Tbs;

            var tbs = await _loadTbsAsync(cancellationToken);
            UpdateTbs(tbs);
            return account.Tbs!;
        }
        finally
        {
            _tbsLock.Release();
        }
    }

    internal async Task<string> EnsureTbsAsync(string operationName, CancellationToken cancellationToken = default)
    {
        var tbs = await GetTbsAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(tbs))
            throw TiebaSessionAuthPolicy.CreateMissingSessionStateException(operationName, nameof(Account.Tbs));

        return tbs;
    }

    internal void UpdateTbs(string tbs)
    {
        var account = RequireAuthenticatedAccount(nameof(UpdateTbs));
        if (string.IsNullOrWhiteSpace(tbs))
            throw TiebaSessionAuthPolicy.CreateMissingSessionStateException(nameof(UpdateTbs), nameof(Account.Tbs));

        account.Tbs = tbs;
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
    }

    internal void UpdateZId(string zId)
    {
        var account = RequireAuthenticatedAccount(nameof(UpdateZId));
        if (string.IsNullOrWhiteSpace(zId))
            throw TiebaSessionAuthPolicy.CreateMissingSessionStateException(nameof(UpdateZId), nameof(Account.ZId));

        account.ZId = zId;
    }

    public void Dispose()
    {
        _tbsLock.Dispose();
        if (HttpCore is IDisposable httpCoreDisposable) httpCoreDisposable.Dispose();
        if (WsCore is IDisposable disposable) disposable.Dispose();
    }

    private static Account? CreateAccount(global::AioTieba4DotNet.TiebaOptions options)
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
