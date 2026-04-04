using System.Diagnostics.CodeAnalysis;

namespace AioTieba4DotNet.Tests.Infrastructure.Contracts;

[ExcludeFromCodeCoverage]
public static class OnlineTestProjectTopology
{
    public const string Infrastructure = "AioTieba4DotNet.Tests.Infrastructure";
    public const string Safe = "AioTieba4DotNet.Tests.Online.Safe";
    public const string Restricted = "AioTieba4DotNet.Tests.Online.Restricted";
    public const string Suite = "AioTieba4DotNet.Tests.Online.Suite";

    public static readonly string[] ProjectNames =
    [
        Infrastructure,
        Safe,
        Restricted,
        Suite
    ];

    public static readonly string[] ContractProjectNames =
    [
        Infrastructure,
        Suite
    ];

    public static readonly string[] ScenarioProjectNames =
    [
        Safe,
        Restricted
    ];
}
