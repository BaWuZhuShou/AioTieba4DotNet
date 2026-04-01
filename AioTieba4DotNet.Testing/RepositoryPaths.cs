using System;
using System.IO;

namespace AioTieba4DotNet.Testing;

public static class RepositoryPaths
{
    private const string SolutionFileName = "AioTieba4DotNet.sln";

    public static string FindRepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, SolutionFileName)))
                return current.FullName;
        }

        throw new DirectoryNotFoundException(
            $"Unable to locate repository root from '{AppContext.BaseDirectory}'. Expected to find {SolutionFileName}.");
    }

    public static string GetWorkflowDirectory()
        => Path.Combine(FindRepositoryRoot(), ".github", "workflows");

    public static string GetScriptsDirectory()
        => Path.Combine(FindRepositoryRoot(), "scripts");

    public static string GetEvidenceDirectory()
        => Path.Combine(FindRepositoryRoot(), ".sisyphus", "evidence");

    public static string GetLocalVerificationManifestPath()
        => Path.Combine(GetEvidenceDirectory(), "local-verification.manifest.json");

    public static string GetLocalVerificationManifestSchemaPath()
        => Path.Combine(GetEvidenceDirectory(), "local-verification.manifest.schema.json");

    public static string GetSequencingManifestPath()
        => Path.Combine(FindRepositoryRoot(), "AioTieba4DotNet.Testing", "test-sequencing.manifest.json");

    public static string GetDeterministicTestProjectPath()
        => Path.Combine(FindRepositoryRoot(), "AioTieba4DotNet.Tests.Deterministic",
            "AioTieba4DotNet.Tests.Deterministic.csproj");

    public static string GetIntegrationTestProjectPath()
        => Path.Combine(FindRepositoryRoot(), "AioTieba4DotNet.Tests.Integration",
            "AioTieba4DotNet.Tests.Integration.csproj");

    public static string GetLiveTestProjectPath()
        => Path.Combine(FindRepositoryRoot(), "AioTieba4DotNet.Tests.Live", "AioTieba4DotNet.Tests.Live.csproj");

    public static string GetLegacyMixedTestDirectory()
        => Path.Combine(FindRepositoryRoot(), "AioTieba4DotNet.Tests");
}
