using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Transport.WebSockets;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.GetThreads;

/// <summary>
///     获取贴吧主题帖列表的 API (FRS页)
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
/// <param name="wsCore">Websocket 核心组件</param>
internal class GetThreads(ITiebaHttpCore httpCore, ITiebaWsCore wsCore)
{
    private const int Cmd = 301001;

    private static byte[] PackProto(string fname, int pn, int rn, int sort, int isGood)
    {
        var frsPageResIdl = new FrsPageReqIdl
        {
            Data = new FrsPageReqIdl.Types.DataReq
            {
                Common = new CommonReq { ClientType = 2, ClientVersion = Const.MainVersion },
                Kw = fname,
                Pn = pn == 1 ? 0 : pn,
                Rn = rn,
                RnNeed = rn + 5,
                IsGood = isGood,
                SortType = sort
            }
        };

        return frsPageResIdl.ToByteArray();
    }

    private static Threads ParseBody(byte[] body)
    {
        var resProto = FrsPageResIdl.Parser.ParseFrom(body);
        ApiResponseValidator.CheckError(resProto.Error.Errorno, resProto.Error.Errmsg);

        var dataForum = resProto.Data;

        return ThreadsMapper.FromTbData(dataForum);
    }

    private static byte[] ExtractWsBody(WSRes response)
    {
        var body = response.Payload?.Data;
        if (body == null || body.IsEmpty)
            throw new TiebaWebSocketUnavailableException(
                "WebSocket returned an empty get-threads payload.");

        return body.ToByteArray();
    }

    private static Threads ParseWsBody(byte[] body)
    {
        try
        {
            return ParseBody(body);
        }
        catch (InvalidProtocolBufferException exception)
        {
            throw new TiebaWebSocketUnavailableException(
                "WebSocket returned an invalid get-threads payload.", exception);
        }
    }

    /// <summary>
    ///     通过 HTTP 获取贴吧主题帖列表
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页请求数量</param>
    /// <param name="sort">排序方式</param>
    /// <param name="isGood">是否只看精品贴 (1:是, 0:否)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>主题帖列表实体</returns>
    public async Task<Threads> RequestHttpAsync(string fname, int pn, int rn, int sort, int isGood,
        CancellationToken cancellationToken = default)
    {
        var data = PackProto(fname, pn, rn, sort, isGood);
        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/f/frs/page") { Query = $"cmd={Cmd}" }.Uri;

        var result = await httpCore.SendAppProtoAsync(requestUri, data, cancellationToken);
        return ParseBody(result);
    }

    /// <summary>
    ///     通过 Websocket 获取贴吧主题帖列表
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页请求数量</param>
    /// <param name="sort">排序方式</param>
    /// <param name="isGood">是否只看精品贴 (1:是, 0:否)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>主题帖列表实体</returns>
    public async Task<Threads> RequestWsAsync(string fname, int pn, int rn, int sort, int isGood,
        CancellationToken cancellationToken = default)
    {
        var data = PackProto(fname, pn, rn, sort, isGood);
        var response = await wsCore.SendAsync(Cmd, data, cancellationToken: cancellationToken);
        return ParseWsBody(ExtractWsBody(response));
    }
}
