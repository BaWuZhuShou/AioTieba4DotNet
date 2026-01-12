using AioTieba4DotNet.Entities;
using AioTieba4DotNet.Enums;

namespace AioTieba4DotNet.Api.GetUInfoGetUserInfoApp.Entities;

/// <summary>
///     用户信息 (App 接口)
/// </summary>
public class UserInfoGuInfoApp : UserInfo
{
    /// <summary>
    ///     从贴吧原始数据转换
    /// </summary>
    /// <param name="dataProto">Protobuf 用户信息数据</param>
    /// <returns>用户信息实体</returns>
    internal static UserInfoGuInfoApp FromTbData(User dataProto)
    {
        var dataProtoPortrait = dataProto.Portrait;
        if (dataProtoPortrait.Contains('?')) dataProtoPortrait = dataProtoPortrait[..^13];

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
