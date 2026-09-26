namespace AioTieba4DotNet.Protocols;

internal interface IForumIdentityResolver
{
    Task<ulong> GetFidAsync(string fname, CancellationToken cancellationToken = default);

    Task<string> GetFnameAsync(ulong fid, CancellationToken cancellationToken = default);
}
