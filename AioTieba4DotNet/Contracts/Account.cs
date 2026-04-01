namespace AioTieba4DotNet.Contracts;

public sealed class Account
{
    public Account(string bduss = "", string stoken = "")
    {
        Bduss = bduss;
        Stoken = stoken;
    }

    public string Bduss { get; init; }

    public string Stoken { get; init; }

    public TiebaOptions ToTiebaOptions() => new()
    {
        Bduss = Bduss,
        Stoken = Stoken
    };
}
