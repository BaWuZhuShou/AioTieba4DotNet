#nullable enable
namespace AioTieba4DotNet.Tests.Infrastructure.Execution;

public enum OnlineExecutionCapability
{
    None = 0,
    Authenticated,
    Messaging,
    Moderation,
    Admin
}
