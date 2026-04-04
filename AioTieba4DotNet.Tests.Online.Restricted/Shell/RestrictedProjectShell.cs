using System.Diagnostics.CodeAnalysis;
using AioTieba4DotNet.Tests.Infrastructure.Contracts;

namespace AioTieba4DotNet.Tests.Online.Restricted;

[ExcludeFromCodeCoverage]
public static class RestrictedProjectShell
{
    public const string ProjectName = OnlineTestProjectTopology.Restricted;
    public const string RequiredTierCategory = OnlineTestTierCategories.Restricted;
}
