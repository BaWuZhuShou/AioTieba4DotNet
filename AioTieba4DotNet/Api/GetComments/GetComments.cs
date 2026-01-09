using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.GetComments.Entities;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using AioTieba4DotNet.Enums;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.GetComments;

public class GetComments(ITiebaHttpCore httpCore, ITiebaWsCore wsCore, TiebaRequestMode mode = TiebaRequestMode.Http)
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
        var code = resProto.Error.Errorno;
        if (code != 0)
        {
            throw new TieBaServerException(code, resProto.Error.Errmsg ?? string.Empty);
        }

        return Comments.FromTbData(resProto.Data);
    }

    public async Task<Comments> RequestAsync(long tid, long pid, int pn, bool isComment)
    {
        if (mode == TiebaRequestMode.Websocket)
        {
            try
            {
                return await RequestWsAsync(tid, pid, pn, isComment);
            }
            catch (NotImplementedException)
            {
            }
        }

        return await RequestHttpAsync(tid, pid, pn, isComment);
    }

    public async Task<Comments> RequestHttpAsync(long tid, long pid, int pn, bool isComment)
    {
        var data = PackProto(tid, pid, pn, isComment, httpCore.Account?.Bduss);
        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/f/pb/floor")
        {
            Query = $"cmd={Cmd}"
        }.Uri;

        var responseMessage = await httpCore.PackProtoRequestAsync(requestUri, data);
        var result = await responseMessage.Content.ReadAsByteArrayAsync();
        return ParseBody(result);
    }

    public async Task<Comments> RequestWsAsync(long tid, long pid, int pn, bool isComment)
    {
        var data = PackProto(tid, pid, pn, isComment, wsCore.Account?.Bduss);
        var response = await wsCore.SendAsync(Cmd, data);
        return ParseBody(response.Payload.Data.ToByteArray());
    }
}
