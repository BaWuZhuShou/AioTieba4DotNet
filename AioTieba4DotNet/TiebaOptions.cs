using AioTieba4DotNet.Enums;

namespace AioTieba4DotNet;

public class TiebaOptions
{
    public string? Bduss { get; set; }
    public string? Stoken { get; set; }
    public TiebaRequestMode RequestMode { get; set; } = TiebaRequestMode.Http;
}
