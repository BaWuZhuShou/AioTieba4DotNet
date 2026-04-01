using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Transport;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.SendMsg;

[RequireBduss]
[PythonApi("aiotieba.api.send_msg")]
internal sealed class SendMsg(ITiebaWsCore wsCore)
{
    private const int Cmd = 205001;

    public async Task<long> RequestAsync(long userId, string content, long recordId,
        CancellationToken cancellationToken = default)
    {
        var data = PackProto(userId, content, recordId);
        var response = await wsCore.SendAsync(Cmd, data, cancellationToken: cancellationToken);
        return ParseResponse(response.Payload.Data.ToByteArray());
    }

    private static byte[] PackProto(long userId, string content, long recordId)
    {
        var request = new CommitPersonalMsgReqIdl
        {
            Data = new CommitPersonalMsgReqIdl.Types.DataReq
            {
                ToUid = userId,
                Content = content,
                MsgType = 1,
                RecordId = recordId
            }
        };

        return request.ToByteArray();
    }

    private static long ParseResponse(byte[] body)
    {
        var response = CommitPersonalMsgResIdl.Parser.ParseFrom(body);
        ApiResponseValidator.CheckError(response.Error.Errorno, response.Error.Errmsg);
        ApiResponseValidator.CheckError(response.Data.BlockInfo?.BlockErrno ?? 0,
            response.Data.BlockInfo?.BlockErrmsg ?? string.Empty);
        return response.Data.MsgId;
    }
}
