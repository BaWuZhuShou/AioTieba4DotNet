#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace AioTieba4DotNet.Tests.Platform.Contracts;

[ExcludeFromCodeCoverage]
public sealed record OnlineOrderedSuiteStage(
    int Order,
    string ProjectName,
    string SuiteCategory,
    string TierCategory,
    string FeatureCategory,
    string StageCategory,
    string? CapabilityCategory,
    string Description)
{
    public string TestCategoryFilter =>
        $"TestCategory={SuiteCategory}&TestCategory={TierCategory}&TestCategory={StageCategory}";
}

[ExcludeFromCodeCoverage]
public sealed record OnlineOrderedSuiteDefinition(
    string SuiteCategory,
    string TierCategory,
    bool RequiresExplicitSelection,
    IReadOnlyList<OnlineOrderedSuiteStage> Stages)
{
    public IReadOnlyList<string> OrderedStageCategories =>
        Stages
            .OrderBy(static stage => stage.Order)
            .Select(static stage => stage.StageCategory)
            .ToArray();
}
