using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.GetComments.Entities;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using AioTieba4DotNet.Enums;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.GetComments;

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
                    BDUSS = bduss ?? string.Empty,
                    ClientType = 2,
                    ClientVersion = Const.MainVersion
                },
                Kz = tid,
                Pn = pn
            }
        };
        if (isComment)
        {
            reqProto.Data.Spid = pid;
        }
        else
        {
            reqProto.Data.Pid = pid;
        }

        return reqProto.ToByteArray();
    }

    private static Comments ParseBody(byte[] body)
    {
        var resProto = PbFloorResIdl.Parser.ParseFrom(body);
        CheckError(resProto.Error.Errorno, resProto.Error.Errmsg);

        return Comments.FromTbData(resProto.Data);
    }

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
        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/f/pb/floor")
        {
            Query = $"cmd={Cmd}"
        }.Uri;

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
