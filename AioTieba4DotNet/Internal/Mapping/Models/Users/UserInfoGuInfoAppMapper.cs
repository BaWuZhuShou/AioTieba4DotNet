using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Users;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class UserInfoGuInfoAppMapper
{
    internal static UserInfoGuInfoApp FromTbData(User dataProto)
    {
        var dataProtoPortrait = dataProto.Portrait;

        if (dataProtoPortrait.Contains('?'))
            dataProtoPortrait = dataProtoPortrait[..^13];

        return new UserInfoGuInfoApp
        {
            UserId = dataProto.Id,
            Portrait = dataProtoPortrait,
            UserName = dataProto.Name,
            NickNameOld = dataProto.NameShow,
            Gender = (Gender)dataProto.Gender,
            IsVip = dataProto.VipInfo.VStatus != 0,
            IsGod = dataProto.NewGodData.Status != 0,
            Ip = dataProto.IpAddress
        };
    }
}
