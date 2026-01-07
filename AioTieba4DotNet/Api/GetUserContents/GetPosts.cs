using AioTieba4DotNet.Api.GetUserContents.Entities;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.GetUserContents;

public class GetPosts(HttpCore httpCore)
{
    private const int Cmd = 303002;

    private static byte[] PackProto(Account account, int userId, uint pn, uint rn, string version)
    {
        var userPostReqIdl = new UserPostReqIdl()
        {
            Data = new UserPostReqIdl.Types.DataReq()
            {
                Common = new CommonReq()
                {
                    BDUSS = account.Bduss,
                    ClientVersion = version,
                },
                UserId = userId,
                Pn = pn,
                Rn = rn,
                NeedContent = 1
            }
        };
        return userPostReqIdl.ToByteArray();
    }

    private static UserPostss ParseBody(byte[] body)
    {
        var resProto = UserPostResIdl.Parser.ParseFrom(body);
        var code = resProto.Error.Errorno;
        if (code != 0)
        {
            throw new TieBaServerException(code, resProto.Error.Errmsg ?? string.Empty);
        }

        var dataForum = resProto.Data;
        return UserPostss.FromTbData(dataForum);
    }

    public async Task<UserPostss> RequestAsync(int userId, uint pn, uint rn, string version)
    {
        var data = PackProto(httpCore.Account!, userId, pn, rn, version);
        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/u/feed/userpost")
        {
            Query = $"cmd={Cmd}"
        }.Uri;

        var responseMessage = await httpCore.PackProtoRequest(requestUri, data);
        var result = await responseMessage.Content.ReadAsByteArrayAsync();
        return ParseBody(result);
    }
}