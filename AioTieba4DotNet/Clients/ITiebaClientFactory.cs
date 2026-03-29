namespace AioTieba4DotNet;

public interface ITiebaClientFactory
{
    ITiebaClient CreateClient(TiebaOptions options);

    ITiebaClient CreateClient(string bduss, string? stoken = null);
}
