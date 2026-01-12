using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.GetThreads.Entities;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Enums;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.GetThreads;

/// <summary>
///     获取贴吧主题帖列表的 API (FRS页)
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
/// <param name="wsCore">Websocket 核心组件</param>
/// <param name="mode">请求模式</param>
[PythonApi("aiotieba.api.get_threads")]
internal class GetThreads(ITiebaHttpCore httpCore, ITiebaWsCore wsCore, TiebaRequestMode mode = TiebaRequestMode.Http)
    : ProtoApiWsBase<Threads>(httpCore, wsCore, mode)
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
        CheckError(resProto.Error.Errorno, resProto.Error.Errmsg);

        var dataForum = resProto.Data;

        return Threads.FromTbData(dataForum);
    }

    /// <summary>
    ///     发送获取贴吧主题帖列表请求
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页请求数量</param>
    /// <param name="sort">
    ///     排序方式
    ///     对于有热门分区的贴吧: 0:热门排序(HOT), 1:按发布时间(CREATE), 2:关注的人(FOLLOW), 3/4:热门排序(HOT), >=5:按回复时间(REPLY)
    ///     对于无热门分区的贴吧: 0:按回复时间(REPLY), 1:按发布时间(CREATE), 2:关注的人(FOLLOW), >=3:按回复时间(REPLY)
    /// </param>
    /// <param name="isGood">是否只看精品贴 (1:是, 0:否)</param>
    /// <returns>主题帖列表实体</returns>
    public async Task<Threads> RequestAsync(string fname, int pn, int rn, int sort, int isGood)
    {
        return await ExecuteAsync(
            () => RequestHttpAsync(fname, pn, rn, sort, isGood),
            () => RequestWsAsync(fname, pn, rn, sort, isGood)
        );
    }

    /// <summary>
    ///     通过 HTTP 获取贴吧主题帖列表
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页请求数量</param>
    /// <param name="sort">排序方式</param>
    /// <param name="isGood">是否只看精品贴 (1:是, 0:否)</param>
    /// <returns>主题帖列表实体</returns>
    public async Task<Threads> RequestHttpAsync(string fname, int pn, int rn, int sort, int isGood)
    {
        var data = PackProto(fname, pn, rn, sort, isGood);
        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/f/frs/page") { Query = $"cmd={Cmd}" }.Uri;

        var result = await HttpCore.SendAppProtoAsync(requestUri, data);
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
    /// <returns>主题帖列表实体</returns>
    public async Task<Threads> RequestWsAsync(string fname, int pn, int rn, int sort, int isGood)
    {
        var data = PackProto(fname, pn, rn, sort, isGood);
        var response = await WsCore.SendAsync(Cmd, data);
        return ParseBody(response.Payload.Data.ToByteArray());
    }
}
