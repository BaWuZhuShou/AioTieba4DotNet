using System;

namespace AioTieba4DotNet.Enums;

[Flags]
public enum BlacklistType
{
    None = 0,
    Follow = 1,
    Interact = 2,
    Chat = 4,
    All = Follow | Interact | Chat
}
