using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Api.GetComments.Entities;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using AioTieba4DotNet.Enums;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.GetComments;

/// <summary>
/// 获取楼中楼回复的 API
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
/// <param name="wsCore">Websocket 核心组件</param>
/// <param name="mode">请求模式</param>
[PythonApi("aiotieba.api.get_comments")]
public class GetComments(ITiebaHttpCore httpCore, ITiebaWsCore wsCore, TiebaRequestMode mode = TiebaRequestMode.Http)
    : ProtoApiWsBase<Comments>(httpCore, wsCore, mode)
{
    private const int Cmd = 302002;

    private static byte[] PackProto(long tid, long pid, int pn, bool isComment, string? bduss)
    {
        var reqProto = new PbFloorReqIdl
        {
            Data = new PbFloorReqIdl.Types.DataReq
            {
                Common = new CommonReq
                {
                    BDUSS = bduss ?? string.Empty, ClientType = 2, ClientVersion = Const.MainVersion
                },
                Kz = tid,
                Pn = pn
            }
        };
        if (isComment)
            reqProto.Data.Spid = pid;
        else
            reqProto.Data.Pid = pid;

        return reqProto.ToByteArray();
    }

    private static Comments ParseBody(byte[] body)
    {
        var resProto = PbFloorResIdl.Parser.ParseFrom(body);
        CheckError(resProto.Error.Errorno, resProto.Error.Errmsg);

        return Comments.FromTbData(resProto.Data);
    }

    /// <summary>
    /// 发送获取楼中楼回复请求
    /// </summary>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="pid">回复 ID (pid) 或楼中楼回复 ID (spid)</param>
    /// <param name="pn">页码</param>
    /// <param name="isComment">如果 pid 是楼中楼回复 ID (spid) 则为 true</param>
    /// <returns>楼中楼回复列表</returns>
    public async Task<Comments> RequestAsync(long tid, long pid, int pn, bool isComment)
    {
        return await ExecuteAsync(
            () => RequestHttpAsync(tid, pid, pn, isComment),
            () => RequestWsAsync(tid, pid, pn, isComment)
        );
    }

    public async Task<Comments> RequestHttpAsync(long tid, long pid, int pn, bool isComment)
    {
        var data = PackProto(tid, pid, pn, isComment, HttpCore.Account?.Bduss);
        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/f/pb/floor") { Query = $"cmd={Cmd}" }.Uri;

        var result = await HttpCore.SendAppProtoAsync(requestUri, data);
        return ParseBody(result);
    }

    public async Task<Comments> RequestWsAsync(long tid, long pid, int pn, bool isComment)
    {
        var data = PackProto(tid, pid, pn, isComment, WsCore.Account?.Bduss);
        var response = await WsCore.SendAsync(Cmd, data);
        return ParseBody(response.Payload.Data.ToByteArray());
    }
}
