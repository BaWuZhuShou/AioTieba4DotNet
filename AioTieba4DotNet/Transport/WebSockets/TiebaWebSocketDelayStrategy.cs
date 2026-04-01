namespace AioTieba4DotNet.Transport.WebSockets;

internal interface ITiebaWebSocketDelayStrategy
{
    Task DelayAsync(TimeSpan interval, CancellationToken cancellationToken);
}

internal sealed class SystemTiebaWebSocketDelayStrategy : ITiebaWebSocketDelayStrategy
{
    internal static SystemTiebaWebSocketDelayStrategy Instance { get; } = new();

    public Task DelayAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        return Task.Delay(interval, cancellationToken);
    }
}
