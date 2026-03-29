namespace AioTieba4DotNet;

public class TiebaOptions
{
    public string? Bduss { get; set; }

    public string? Stoken { get; set; }

    public TiebaTransportMode TransportMode { get; set; } = TiebaTransportMode.Auto;

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    public int MaxReadRetryAttempts { get; set; }
}
