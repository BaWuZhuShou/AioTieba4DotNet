#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace AioTieba4DotNet.Tests.Platform.Contracts;

[ExcludeFromCodeCoverage]
public sealed record OnlineFeatureMetadata(
    string FeatureCategory,
    string TierCategory,
    string StageCategory,
    string? CapabilityCategory);
