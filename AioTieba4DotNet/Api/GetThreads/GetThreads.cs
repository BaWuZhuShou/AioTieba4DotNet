using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Models;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.GetThreads;

/// <summary>
///     获取贴吧主题帖列表的 API (FRS页)
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
/// <param name="wsCore">Websocket 核心组件</param>
[PythonApi("aiotieba.api.get_threads")]
internal class GetThreads(ITiebaHttpCore httpCore, ITiebaWsCore wsCore)
{
    private readonly ITiebaHttpCore _httpCore = httpCore;
    private readonly ITiebaWsCore _wsCore = wsCore;

    private const int Cmd = 301001;

    private static byte[] PackProto(string fname, int pn, int rn, int sort, int isGood)
    {
        var frsPageResIdl = new FrsPageReqIdl
        {
            Data = new FrsPageReqIdl.Types.DataReq
            {
                Common = new CommonReq { ClientType = 2, ClientVersion = Const.MainVersion },
                Kw = fname,
                Rn = rn,
                RnNeed = rn + 5,
                IsGood = isGood,
                SortType = sort,
                LoadType = 1
            }
        };
        if (pn != 1) frsPageResIdl.Data.Pn = pn;

        return frsPageResIdl.ToByteArray();
    }

    private static Threads ParseBody(byte[] body)
    {
        var resProto = FrsPageResIdl.Parser.ParseFrom(body);
        ApiResponseValidator.CheckError(resProto.Error.Errorno, resProto.Error.Errmsg);

        var dataForum = resProto.Data;

        return Internal.Mapping.ThreadsMapper.FromTbData(dataForum);
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
        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/f/frs/page") { Query = $"cmd={Cmd}" }.Uri;

        var result = await _httpCore.SendAppProtoAsync(requestUri, data, cancellationToken);
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
        var response = await _wsCore.SendAsync(Cmd, data, cancellationToken: cancellationToken);
        return ParseBody(response.Payload.Data.ToByteArray());
    }
}
