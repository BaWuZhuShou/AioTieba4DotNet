using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Models;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.GetComments;

/// <summary>
///     获取楼中楼回复的 API
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
/// <param name="wsCore">Websocket 核心组件</param>
[PythonApi("aiotieba.api.get_comments")]
internal class GetComments(ITiebaHttpCore httpCore, ITiebaWsCore wsCore)
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
        ApiResponseValidator.CheckError(resProto.Error.Errorno, resProto.Error.Errmsg);

        return Internal.Mapping.CommentsMapper.FromTbData(resProto.Data);
    }

    /// <summary>
    ///     通过 HTTP 获取楼中楼回复
    /// </summary>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="pid">回复 ID (pid) 或楼中楼回复 ID (spid)</param>
    /// <param name="pn">页码</param>
    /// <param name="isComment">如果 pid 是楼中楼回复 ID (spid) 则为 true</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>楼中楼回复列表</returns>
    public async Task<Comments> RequestHttpAsync(long tid, long pid, int pn, bool isComment,
        CancellationToken cancellationToken = default)
    {
        var data = PackProto(tid, pid, pn, isComment, httpCore.Account?.Bduss);
        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/f/pb/floor") { Query = $"cmd={Cmd}" }.Uri;

        var result = await httpCore.SendAppProtoAsync(requestUri, data, cancellationToken);
        return ParseBody(result);
    }

    /// <summary>
    ///     通过 Websocket 获取楼中楼回复
    /// </summary>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="pid">回复 ID (pid) 或楼中楼回复 ID (spid)</param>
    /// <param name="pn">页码</param>
    /// <param name="isComment">如果 pid 是楼中楼回复 ID (spid) 则为 true</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>楼中楼回复列表</returns>
    public async Task<Comments> RequestWsAsync(long tid, long pid, int pn, bool isComment,
        CancellationToken cancellationToken = default)
    {
        var data = PackProto(tid, pid, pn, isComment, wsCore.Account?.Bduss);
        var response = await wsCore.SendAsync(Cmd, data, cancellationToken: cancellationToken);
        return ParseBody(response.Payload.Data.ToByteArray());
    }
}
