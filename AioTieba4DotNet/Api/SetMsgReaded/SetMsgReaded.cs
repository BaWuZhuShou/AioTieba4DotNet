using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Transport;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.SetMsgReaded;

[RequireBduss]
[PythonApi("aiotieba.api.set_msg_readed")]
internal sealed class SetMsgReaded(ITiebaWsCore wsCore)
{
    private const int Cmd = 205006;
    private const int ReadMessageType = 22;

    public async Task<bool> RequestAsync(long userId, long groupId, long messageId,
        CancellationToken cancellationToken = default)
    {
        var data = PackProto(userId, groupId, messageId);
        var response = await wsCore.SendAsync(Cmd, data, cancellationToken: cancellationToken);
        ParseResponse(response.Payload.Data.ToByteArray());
        return true;
    }

    private static byte[] PackProto(long userId, long groupId, long messageId)
    {
        var request = new CommitReceivedPmsgReqIdl
        {
            Data = new CommitReceivedPmsgReqIdl.Types.DataReq
            {
                GroupId = groupId, ToUid = userId, MsgType = ReadMessageType, MsgId = messageId
            }
        };

        return request.ToByteArray();
    }

    private static void ParseResponse(byte[] body)
    {
        var response = CommitReceivedPmsgResIdl.Parser.ParseFrom(body);
        ApiResponseValidator.CheckError(response.Error.Errorno, response.Error.Errmsg);
    }
}
