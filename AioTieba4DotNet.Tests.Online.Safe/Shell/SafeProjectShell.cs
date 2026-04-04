using System.Diagnostics.CodeAnalysis;
using AioTieba4DotNet.Tests.Infrastructure.Contracts;

namespace AioTieba4DotNet.Tests.Online.Safe;

[ExcludeFromCodeCoverage]
public static class SafeProjectShell
{
    public const string ProjectName = OnlineTestProjectTopology.Safe;
    public const string DefaultTierCategory = OnlineTestTierCategories.Safe;
}
