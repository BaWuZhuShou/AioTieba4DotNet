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
    /// <summary>
    ///     User ID
    /// </summary>
    UserId = 1,

    /// <summary>
    ///     头像 ID
    /// </summary>
    Portrait = 1 << 1,

    /// <summary>
    ///     用户名
    /// </summary>
    UserName = 1 << 2,

    /// <summary>
    ///     昵称
    /// </summary>
    NickName = 1 << 3,

    /// <summary>
    ///     贴吧 UID
    /// </summary>
    TiebaUid = 1 << 4,

    /// <summary>
    ///     其他信息
    /// </summary>
    Other = 1 << 5,

    /// <summary>
    ///     基础信息
    /// </summary>
    Basic = UserId | Portrait | UserName,

    /// <summary>
    ///     全部信息
    /// </summary>
    All = Basic | NickName | TiebaUid | Other
}
