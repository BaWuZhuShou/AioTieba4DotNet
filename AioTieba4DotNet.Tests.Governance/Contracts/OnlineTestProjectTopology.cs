using System.Diagnostics.CodeAnalysis;

namespace AioTieba4DotNet.Tests.Governance.Contracts;

[ExcludeFromCodeCoverage]
public static class OnlineTestProjectTopology
{
    public const string Platform = "AioTieba4DotNet.Tests.Platform";
    public const string Online = "AioTieba4DotNet.Tests.Online";
    public const string Governance = "AioTieba4DotNet.Tests.Governance";

    public static readonly string[] ProjectNames =
    [
        Platform,
        Online,
        Governance
    ];

    public static readonly string[] TestProjectNames =
    [
        Online,
        Governance
    ];

    public static readonly string[] ContractProjectNames =
    [
        Governance
    ];

    public static readonly string[] ScenarioProjectNames =
    [
        Online
    ];
}
