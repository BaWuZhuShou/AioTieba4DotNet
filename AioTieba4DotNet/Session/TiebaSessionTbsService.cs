namespace AioTieba4DotNet.Session;

internal sealed class TiebaSessionTbsService(
    Func<string, Account> requireAuthenticatedAccount,
    TiebaSessionStateStore stateStore,
    Func<CancellationToken, Task<string>> loadTbsAsync) : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public void Dispose()
    {
        _gate.Dispose();
    }

    internal async Task<string> GetAsync(string operationName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        cancellationToken.ThrowIfCancellationRequested();

        var account = requireAuthenticatedAccount(operationName);
        if (!string.IsNullOrWhiteSpace(account.Tbs))
        {
            stateStore.SetTbs(account.Tbs);
            return account.Tbs;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            account = requireAuthenticatedAccount(operationName);
            if (!string.IsNullOrWhiteSpace(account.Tbs))
            {
                stateStore.SetTbs(account.Tbs);
                return account.Tbs;
            }

            return await LoadAndStoreAsync(account, operationName, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    internal async Task<string> RefreshAsync(string operationName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        cancellationToken.ThrowIfCancellationRequested();

        var account = requireAuthenticatedAccount(operationName);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await LoadAndStoreAsync(account, operationName, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<string> LoadAndStoreAsync(Account account, string operationName,
        CancellationToken cancellationToken)
    {
        var previousState = stateStore.CaptureTbs();
        stateStore.SetTbsInitializing();
        try
        {
            var tbs = await loadTbsAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(tbs))
                throw TiebaSessionAuthPolicy.CreateMissingSessionStateException(operationName, nameof(Account.Tbs));

            account.Tbs = tbs;
            stateStore.SetTbs(tbs);
            return tbs;
        }
        catch
        {
            stateStore.RestoreTbs(previousState.State, previousState.Value);
            throw;
        }
    }
}
