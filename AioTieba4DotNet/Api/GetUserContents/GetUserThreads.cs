using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Transport;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.GetUserContents;

/// <summary>
///     获取用户发布主题帖列表的 API
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
/// <param name="wsCore">Websocket 核心组件</param>
[RequireBduss]
[PythonApi("aiotieba.api.get_user_contents.get_threads")]
internal class GetUserThreads(
    ITiebaHttpCore httpCore,
    ITiebaWsCore wsCore)
{
    private const int Cmd = 303002;

    private static byte[] PackProto(Account account, int userId, uint pn, bool publicOnly)
    {
        var userPostReqIdl = new UserPostReqIdl
        {
            Data = new UserPostReqIdl.Types.DataReq
            {
                Common = new CommonReq
                {
                    ClientType = 2, BDUSS = account.Bduss, ClientVersion = Const.MainVersion
                },
                UserId = userId,
                Pn = pn,
                IsThread = 1,
                NeedContent = 1,
                IsViewCard = publicOnly ? 2 : 1
            }
        };
        return userPostReqIdl.ToByteArray();
    }

    private static UserThreads ParseBody(byte[] body)
    {
        var resProto = UserPostResIdl.Parser.ParseFrom(body);
        ApiResponseValidator.CheckError(resProto.Error.Errorno, resProto.Error.Errmsg);

        return UserThreadsMapper.FromTbData(resProto.Data);
    }

    /// <summary>
    ///     通过 HTTP 获取用户发布主题帖列表
    /// </summary>
    /// <param name="userId">用户 ID (uid)</param>
    /// <param name="pn">页码</param>
    /// <param name="publicOnly">是否只看公开帖子</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户发布主题帖列表实体</returns>
    public async Task<UserThreads> RequestHttpAsync(int userId, uint pn, bool publicOnly,
        CancellationToken cancellationToken = default)
    {
        var data = PackProto(httpCore.Account!, userId, pn, publicOnly);
        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/u/feed/userpost") { Query = $"cmd={Cmd}" }
            .Uri;

        var result = await httpCore.SendAppProtoAsync(requestUri, data, cancellationToken);
        return ParseBody(result);
    }

    /// <summary>
    ///     通过 Websocket 获取用户发布主题帖列表
    /// </summary>
    /// <param name="userId">用户 ID (uid)</param>
    /// <param name="pn">页码</param>
    /// <param name="publicOnly">是否只看公开帖子</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户发布主题帖列表实体</returns>
    public async Task<UserThreads> RequestWsAsync(int userId, uint pn, bool publicOnly,
        CancellationToken cancellationToken = default)
    {
        var data = PackProto(wsCore.Account!, userId, pn, publicOnly);
        var response = await wsCore.SendAsync(Cmd, data, cancellationToken: cancellationToken);
        return ParseBody(response.Payload.Data.ToByteArray());
    }
}
