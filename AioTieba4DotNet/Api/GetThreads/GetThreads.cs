using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.GetThreads.Entities;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using AioTieba4DotNet.Enums;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.GetThreads;

/// <summary>
/// 
/// </summary>
/// <param name="httpCore"></param>
/// <param name="wsCore"></param>
/// <param name="mode"></param>
public class GetThreads(ITiebaHttpCore httpCore, ITiebaWsCore wsCore, TiebaRequestMode mode = TiebaRequestMode.Http)
{
    private const int Cmd = 301001;

    private static byte[] PackProto(string fname, int pn, int rn, int sort, int isGood)
    {
        var frsPageResIdl = new FrsPageReqIdl()
        {
            Data = new FrsPageReqIdl.Types.DataReq
            {
                Common = new CommonReq
                {
                    ClientType = 2,
                    ClientVersion = Const.MainVersion
                },
                Kw = fname,
                Pn = pn == 1 ? 0 : pn,
                Rn = rn,
                RnNeed = rn + 5,
                IsGood = isGood,
                SortType = sort,
                LoadType = 1,
            }
        };
        return frsPageResIdl.ToByteArray();
    }

    private static Threads ParseBody(byte[] body)
    {
        var resProto = FrsPageResIdl.Parser.ParseFrom(body);
        var code = resProto.Error.Errorno;
        if (code != 0)
        {
            throw new TieBaServerException(code, resProto.Error.Errmsg ?? string.Empty);
        }

        var dataForum = resProto.Data;

        return Threads.FromTbData(dataForum);
    }

    /// <summary>
    /// 异步请求
    /// </summary>
    /// <param name="fname">贴吧名</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页条数</param>
    /// <param name="sort">
    /// 排序
    ///对于有热门分区的贴吧 0热门排序(HOT) 1按发布时间(CREATE) 2关注的人(FOLLOW) 34热门排序(HOT) >=5是按回复时间(REPLY)\n
    ///对于无热门分区的贴吧 0按回复时间(REPLY) 1按发布时间(CREATE) 2关注的人(FOLLOW) >=3按回复时间(REPLY)
    /// </param>
    /// <param name="isGood">是否精品贴</param>
    /// <returns></returns>
    public async Task<Threads> RequestAsync(string fname, int pn, int rn, int sort, int isGood)
    {
        if (mode == TiebaRequestMode.Websocket)
        {
            try
            {
                return await RequestWsAsync(fname, pn, rn, sort, isGood);
            }
            catch (NotImplementedException)
            {
                // 强制要求ws但是未实现，回退http
            }
        }

        return await RequestHttpAsync(fname, pn, rn, sort, isGood);
    }

    public async Task<Threads> RequestHttpAsync(string fname, int pn, int rn, int sort, int isGood)
    {
        var data = PackProto(fname, pn, rn, sort, isGood);
        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/f/frs/page")
        {
            Query = $"cmd={Cmd}"
        }.Uri;

        var responseMessage = await httpCore.PackProtoRequestAsync(requestUri, data);
        var result = await responseMessage.Content.ReadAsByteArrayAsync();
        return ParseBody(result);
    }

    public async Task<Threads> RequestWsAsync(string fname, int pn, int rn, int sort, int isGood)
    {
        var data = PackProto(fname, pn, rn, sort, isGood);
        var response = await wsCore.SendAsync(Cmd, data);
        return ParseBody(response.Payload.Data.ToByteArray());
    }
}
