using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AioTieba4DotNet.Testing;

public sealed record TestSequenceStage(string Name, string Description, IReadOnlyList<string> Lanes);

public sealed record TestSequencingManifest(int Version, IReadOnlyList<TestSequenceStage> Stages)
{
    public static readonly IReadOnlyList<string> ExpectedBusinessOrder =
    [
        "ForumFoundation",
        "ForumExtensions",
        "ThreadRead",
        "ThreadWriteModeration",
        "UserSocial",
        "MessagingClient",
        "Cleanup"
    ];

    public static TestSequencingManifest LoadDefault()
    {
        var manifestPath = RepositoryPaths.GetSequencingManifestPath();
        var json = File.ReadAllText(manifestPath);
        var manifest = JsonSerializer.Deserialize<TestSequencingManifest>(json, JsonOptions);

        if (manifest is null)
            throw new InvalidDataException($"Unable to deserialize test sequencing manifest '{manifestPath}'.");

        return manifest;
    }

    public IReadOnlyList<string> GetStageNames()
    {
        return Stages.Select(static stage => stage.Name).ToArray();
    }

    public IReadOnlyList<TestSequenceStage> GetStagesForLane(string lane)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lane);

        return Stages
            .Where(stage => stage.Lanes.Contains(lane, StringComparer.OrdinalIgnoreCase))
            .ToArray();
    }

    public void ValidateExpectedBusinessOrder()
    {
        if (!GetStageNames().SequenceEqual(ExpectedBusinessOrder, StringComparer.Ordinal))
            throw new InvalidDataException(
                "The test sequencing manifest does not match the expected business order.");
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
}
