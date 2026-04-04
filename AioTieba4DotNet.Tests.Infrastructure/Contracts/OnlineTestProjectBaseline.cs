#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace AioTieba4DotNet.Tests.Infrastructure.Contracts;

[ExcludeFromCodeCoverage]
public static class OnlineTestProjectBaseline
{
    public const string OnlineTestProjectProperty = "IsOnlineTestProject";
    public const string OnlineScenarioProjectProperty = "IsOnlineScenarioProject";
    public const string OnlineContractProjectProperty = "IsOnlineContractProject";

    public static readonly string[] CentralizedPropertyNames =
    [
        "IsTestProject",
        "IsPackable",
        "EnableDefaultCompileItems"
    ];

    public static readonly string[] CentralizedPackageReferences =
    [
        "JetBrains.Annotations",
        "Microsoft.NET.Test.Sdk",
        "MSTest.TestAdapter",
        "MSTest.TestFramework"
    ];

    public const string SourceCompileInclude = "**\\*.cs";
    public const string SourceCompileRemove = "bin\\**\\*.cs;obj\\**\\*.cs";
}
