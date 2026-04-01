namespace AioTieba4DotNet.Models.Messages;

public enum WsStatus
{
    Unknown = 0,
    Disconnected = 1,
    Connected = 2
}

public enum GroupType
{
    Unknown = 0,
    PrivateMessage = 1,
    Chatroom = 2
}

public enum MsgType
{
    Unknown = 0,
    Text = 1,
    System = 2
}
