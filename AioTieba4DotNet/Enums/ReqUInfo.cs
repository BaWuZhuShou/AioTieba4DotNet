namespace AioTieba4DotNet.Enums;

/// <summary>
///     使用该枚举类指定待获取的用户信息字段
///     Note:
///     各bit位的含义由高到低分别为 OTHER, TIEBA_UID, NICK_NAME, USER_NAME, PORTRAIT, USER_ID\n
///     其中BASIC = USER_ID | PORTRAIT | USER_NAME
/// </summary>
[Flags]
public enum ReqUInfo
{
    UserId = 1,
    Portrait = 1 << 1,
    UserName = 1 << 2,
    NickName = 1 << 3,
    TiebaUid = 1 << 4,
    Other = 1 << 5,
    Basic = UserId | Portrait | UserName,
    All = Basic | NickName | TiebaUid | Other
}
