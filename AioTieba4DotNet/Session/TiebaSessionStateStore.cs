namespace AioTieba4DotNet.Session;

internal sealed class TiebaSessionStateStore(Account? account)
{
    private readonly object _syncRoot = new();
    private TiebaSessionState _currentState = TiebaSessionState.FromAccount(account);

    internal TiebaSessionState CurrentState
    {
        get
        {
            lock (_syncRoot)
            {
                return _currentState;
            }
        }
    }

    internal (TiebaSessionResourceState State, string? Value) CaptureTbs()
    {
        lock (_syncRoot)
        {
            return (_currentState.TbsState, _currentState.Tbs);
        }
    }

    internal void SetTbsInitializing()
    {
        lock (_syncRoot)
        {
            _currentState = _currentState with { TbsState = TiebaSessionResourceState.Initializing };
        }
    }

    internal void RestoreTbs(TiebaSessionResourceState state, string? tbs)
    {
        lock (_syncRoot)
        {
            _currentState = _currentState with { TbsState = state, Tbs = tbs };
        }
    }

    internal void SetTbs(string tbs)
    {
        lock (_syncRoot)
        {
            _currentState = _currentState with
            {
                Tbs = tbs,
                TbsState = TiebaSessionResourceState.Ready
            };
        }
    }

    internal (TiebaSessionResourceState State, string? Value) CaptureZId()
    {
        lock (_syncRoot)
        {
            return (_currentState.ZIdState, _currentState.ZId);
        }
    }

    internal void SetZIdInitializing()
    {
        lock (_syncRoot)
        {
            _currentState = _currentState with { ZIdState = TiebaSessionResourceState.Initializing };
        }
    }

    internal void RestoreZId(TiebaSessionResourceState state, string? zId)
    {
        lock (_syncRoot)
        {
            _currentState = _currentState with { ZIdState = state, ZId = zId };
        }
    }

    internal void SetZId(string zId)
    {
        lock (_syncRoot)
        {
            _currentState = _currentState with
            {
                ZId = zId,
                ZIdState = TiebaSessionResourceState.Ready
            };
        }
    }

    internal (TiebaSessionResourceState State, string? ClientId, string? SampleId) CaptureClientSync()
    {
        lock (_syncRoot)
        {
            return (_currentState.ClientState, _currentState.ClientId, _currentState.SampleId);
        }
    }

    internal void SetClientSyncInitializing()
    {
        lock (_syncRoot)
        {
            _currentState = _currentState with { ClientState = TiebaSessionResourceState.Initializing };
        }
    }

    internal void RestoreClientSync(TiebaSessionResourceState state, string? clientId, string? sampleId)
    {
        lock (_syncRoot)
        {
            _currentState = _currentState with
            {
                ClientState = state,
                ClientId = clientId,
                SampleId = sampleId
            };
        }
    }

    internal void SetClientIdentifiers(string clientId, string sampleId)
    {
        lock (_syncRoot)
        {
            _currentState = _currentState with
            {
                ClientState = TiebaSessionResourceState.Ready,
                ClientId = clientId,
                SampleId = sampleId
            };
        }
    }

    internal TiebaSessionResourceState CaptureWebSocketState()
    {
        lock (_syncRoot)
        {
            return _currentState.WebSocketState;
        }
    }

    internal void SetWebSocketInitializing()
    {
        lock (_syncRoot)
        {
            _currentState = _currentState with { WebSocketState = TiebaSessionResourceState.Initializing };
        }
    }

    internal void RestoreWebSocketState(TiebaSessionResourceState state)
    {
        lock (_syncRoot)
        {
            _currentState = _currentState with { WebSocketState = state };
        }
    }

    internal void SetWebSocketReady()
    {
        lock (_syncRoot)
        {
            _currentState = _currentState with { WebSocketState = TiebaSessionResourceState.Ready };
        }
    }
}
