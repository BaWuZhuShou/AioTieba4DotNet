#nullable enable
using AioTieba4DotNet.Tests.Infrastructure.Configuration;

namespace AioTieba4DotNet.Tests.Infrastructure.Execution;

public sealed record OnlineExecutionScope(
    OnlineTestEnvironment Environment,
    OnlineExecutionCapability Capability,
    bool IsRestricted,
    OnlineTestCompensationScope Compensation)
{
    public OnlineSafeProfile Safe => Environment.Safe;

    public OnlineRestrictedProfile Restricted => Environment.Restricted;
}
