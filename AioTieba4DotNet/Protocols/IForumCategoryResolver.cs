namespace AioTieba4DotNet.Protocols;

internal interface IForumCategoryResolver
{
    Task<int> GetCidAsync(string fname, string cname = "", CancellationToken cancellationToken = default);
}
