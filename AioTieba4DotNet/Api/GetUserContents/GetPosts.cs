using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Transport;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.GetUserContents;

/// <summary>
///     获取用户发布回复列表的 API
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
/// <param name="wsCore">Websocket 核心组件</param>
internal class GetPosts(
    ITiebaHttpCore httpCore,
    ITiebaWsCore wsCore)
{
    private const int Cmd = 303002;

    private static byte[] PackProto(Account account, int userId, uint pn, uint rn, string version)
    {
        var userPostReqIdl = new UserPostReqIdl
        {
            Data = new UserPostReqIdl.Types.DataReq
            {
                Common = new CommonReq { ClientType = 2, BDUSS = account.Bduss, ClientVersion = version },
                UserId = userId,
                Pn = pn,
                Rn = rn,
                NeedContent = 1
            }
        };
        return userPostReqIdl.ToByteArray();
    }

    private static UserPostGroups ParseBody(byte[] body)
    {
        var resProto = UserPostResIdl.Parser.ParseFrom(body);
        ApiResponseValidator.CheckError(resProto.Error.Errorno, resProto.Error.Errmsg);

        var dataForum = resProto.Data;
        return UserPostGroupsMapper.FromTbData(dataForum);
    }

    /// <summary>
    ///     通过 HTTP 获取用户发布回复列表
    /// </summary>
    /// <param name="userId">用户 ID (uid)</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页请求数量</param>
    /// <param name="version">客户端版本号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>回复列表实体</returns>
    public async Task<UserPostGroups> RequestHttpAsync(int userId, uint pn, uint rn, string version,
        CancellationToken cancellationToken = default)
    {
        var data = PackProto(httpCore.Account!, userId, pn, rn, version);
        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/u/feed/userpost") { Query = $"cmd={Cmd}" }
            .Uri;

        var result = await httpCore.SendAppProtoAsync(requestUri, data, cancellationToken);
        return ParseBody(result);
    }

    /// <summary>
    ///     通过 Websocket 获取用户发布回复列表
    /// </summary>
    /// <param name="userId">用户 ID (uid)</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页请求数量</param>
    /// <param name="version">客户端版本号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>回复列表实体</returns>
    public async Task<UserPostGroups> RequestWsAsync(int userId, uint pn, uint rn, string version,
        CancellationToken cancellationToken = default)
    {
        var data = PackProto(wsCore.Account!, userId, pn, rn, version);
        var response = await wsCore.SendAsync(Cmd, data, cancellationToken: cancellationToken);
        return ParseBody(response.Payload.Data.ToByteArray());
    }
}
