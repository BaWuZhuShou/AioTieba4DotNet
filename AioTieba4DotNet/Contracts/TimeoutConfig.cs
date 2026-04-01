namespace AioTieba4DotNet.Contracts;

public sealed class TimeoutConfig
{
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(30);

    public int MaxReadRetryAttempts { get; init; }
}
