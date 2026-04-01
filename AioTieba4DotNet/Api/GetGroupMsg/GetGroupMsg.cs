using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Messages;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.GetGroupMsg;

[RequireBduss]
[PythonApi("aiotieba.api.get_group_msg")]
internal sealed class GetGroupMsg(ITiebaWsCore wsCore)
{
    private const int Cmd = 202003;

    public async Task<WsMsgGroups> RequestAsync(IReadOnlyList<long> groupIds, IReadOnlyList<long> lastMessageIds,
        int getType, CancellationToken cancellationToken = default)
    {
        var account = wsCore.Account
                      ?? throw new InvalidOperationException(
                          "An authenticated account is required to read websocket groups.");

        var data = PackProto(account, groupIds, lastMessageIds, getType);
        var response = await wsCore.SendAsync(Cmd, data, cancellationToken: cancellationToken);
        return ParseResponse(response.Payload.Data.ToByteArray());
    }

    private static byte[] PackProto(Account account, IReadOnlyList<long> groupIds, IReadOnlyList<long> lastMessageIds,
        int getType)
    {
        var request = new GetGroupMsgReqIdl
        {
            Cuid = $"{account.Cuid}|com.baidu.tieba_mini{Const.PostVersion}",
            Data = new GetGroupMsgReqIdl.Types.DataReq { Gettype = getType.ToString() }
        };

        for (var index = 0; index < groupIds.Count; index++)
            request.Data.GroupMids.Add(new GetGroupMsgReqIdl.Types.DataReq.Types.GroupLastId
            {
                GroupId = groupIds[index], LastMsgId = index < lastMessageIds.Count ? lastMessageIds[index] : 0
            });

        return request.ToByteArray();
    }

    private static WsMsgGroups ParseResponse(byte[] body)
    {
        var response = GetGroupMsgResIdl.Parser.ParseFrom(body);
        ApiResponseValidator.CheckError(response.Error.Errorno, response.Error.Errmsg);
        return WsMsgGroupsMapper.FromTbData(response.Data);
    }
}
