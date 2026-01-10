using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.GetForumDetail.Entities;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.GetForumDetail;

public class GetForumDetail(ITiebaHttpCore httpCore) : ProtoApiBase(httpCore)
{
    private const int Cmd = 303021;

    private static byte[] PackProto(long fid)
    {
        var reqIdl = new GetForumDetailReqIdl()
        {
            Data = new()
            {
                Common = new()
                {
                    ClientType = 2,
                    ClientVersion = Const.MainVersion
                },
               ForumId = fid
            }
        };
        return reqIdl.ToByteArray();
    }

    private static ForumDetail ParseBody(byte[] body)
    {
        var resProto = GetForumDetailResIdl.Parser.ParseFrom(body);
        CheckError(resProto.Error.Errorno, resProto.Error.Errmsg);

        var dataForum = resProto.Data;

        return ForumDetail.FromTbData(dataForum);
    }

    public async Task<ForumDetail> RequestAsync(long fid)
    {
        var data = PackProto(fid);
        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/f/forum/getforumdetail")
        {
            Query = $"cmd={Cmd}"
        }.Uri;

        var result = await HttpCore.SendAppProtoAsync(requestUri, data);
        return ParseBody(result);
    }
}

