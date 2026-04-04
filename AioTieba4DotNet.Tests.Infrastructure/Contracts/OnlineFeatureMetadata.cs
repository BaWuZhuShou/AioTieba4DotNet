#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace AioTieba4DotNet.Tests.Infrastructure.Contracts;

[ExcludeFromCodeCoverage]
public sealed record OnlineFeatureMetadata(
    string FeatureCategory,
    string TierCategory,
    string StageCategory,
    string? CapabilityCategory);
