#nullable enable
using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using AioTieba4DotNet.Tests.Infrastructure.Configuration;

namespace AioTieba4DotNet.Tests.Infrastructure.Execution;

public abstract class OnlineExecutionTestBase : IAsyncDisposable
{
    private readonly OnlineTestCompensationScope _compensation = new();
    private bool _disposed;

    protected OnlineExecutionTestBase()
    {
        Environment = OnlineTestEnvironment.LoadCurrent();
    }

    protected OnlineTestEnvironment Environment { get; }

    protected OnlineCompensationAudit? LastCompensationAudit => _compensation.GetLastAudit();

    protected OnlineTestCompensationScope Compensation => _compensation;

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        Exception? compensationException = null;

        try
        {
            await _compensation.DisposeAsync();
        }
        catch (Exception exception)
        {
            compensationException = exception;
        }
        finally
        {
            var compensationAudit = _compensation.GetLastAudit();
            if (compensationAudit is not null)
                foreach (var line in compensationAudit.ToDisplayLines())
                    Console.WriteLine(line);
        }

        _disposed = true;
        GC.SuppressFinalize(this);

        if (compensationException is not null)
            ExceptionDispatchInfo.Capture(compensationException).Throw();
    }
}

public abstract class OnlineSafeExecutionTestBase : OnlineExecutionTestBase
{
    protected void ExecuteSafe(
        string operationName,
        Action<OnlineExecutionScope> action,
        OnlineExecutionCapability capability = OnlineExecutionCapability.None)
    {
        OnlineExecutionGate.ExecuteSafe(Environment, operationName, action, capability, Compensation);
    }

    protected Task ExecuteSafeAsync(
        string operationName,
        Func<OnlineExecutionScope, Task> action,
        OnlineExecutionCapability capability = OnlineExecutionCapability.None)
    {
        return OnlineExecutionGate.ExecuteSafeAsync(Environment, operationName, action, capability, Compensation);
    }
}

public abstract class OnlineRestrictedExecutionTestBase : OnlineExecutionTestBase
{
    protected void ExecuteRestricted(
        string operationName,
        OnlineExecutionCapability capability,
        Action<OnlineExecutionScope> action)
    {
        OnlineExecutionGate.ExecuteRestricted(Environment, operationName, capability, action, Compensation);
    }

    protected Task ExecuteRestrictedAsync(
        string operationName,
        OnlineExecutionCapability capability,
        Func<OnlineExecutionScope, Task> action)
    {
        return OnlineExecutionGate.ExecuteRestrictedAsync(Environment, operationName, capability, action, Compensation);
    }
}
