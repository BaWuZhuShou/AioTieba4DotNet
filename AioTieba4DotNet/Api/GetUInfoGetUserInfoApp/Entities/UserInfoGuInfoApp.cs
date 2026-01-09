using AioTieba4DotNet.Entities;
using AioTieba4DotNet.Enums;

namespace AioTieba4DotNet.Api.GetUInfoGetUserInfoApp.Entities;

public class UserInfoGuInfoApp : UserInfo
{
    public static UserInfoGuInfoApp FromTbData(User dataProto)
    {
        var dataProtoPortrait = dataProto.Portrait;
        if (dataProtoPortrait.Contains('?'))
        {
            dataProtoPortrait = dataProtoPortrait[..^13];
        }

        return new UserInfoGuInfoApp
        {
            UserId = dataProto.Id,
            Portrait = dataProtoPortrait,
            UserName = dataProto.Name,
            NickNameOld = dataProto.NameShow,
            Gender = (Gender)dataProto.Gender,
            IsVip = dataProto.VipInfo.VStatus != 0,
            IsGod = dataProto.NewGodData.Status != 0,
            Ip = dataProto.IpAddress,
        };
    }
}
