using System;

namespace AioTieba4DotNet.Enums;

/// <summary>
///     黑名单类型
/// </summary>
[Flags]
public enum BlacklistType
{
    /// <summary>
    ///     不拉黑
    /// </summary>
    None = 0,

    /// <summary>
    ///     拉黑关注相关行为
    /// </summary>
    Follow = 1,

    /// <summary>
    ///     拉黑互动相关行为
    /// </summary>
    Interact = 2,

    /// <summary>
    ///     拉黑私聊相关行为
    /// </summary>
    Chat = 4,

    /// <summary>
    ///     拉黑全部行为
    /// </summary>
    All = Follow | Interact | Chat
}
