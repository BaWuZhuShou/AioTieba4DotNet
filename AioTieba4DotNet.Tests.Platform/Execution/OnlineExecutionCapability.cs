#nullable enable
namespace AioTieba4DotNet.Tests.Platform.Execution;

public enum OnlineExecutionCapability
{
    None = 0,
    Authenticated,
    Messaging,
    Moderation,
    Admin
}
