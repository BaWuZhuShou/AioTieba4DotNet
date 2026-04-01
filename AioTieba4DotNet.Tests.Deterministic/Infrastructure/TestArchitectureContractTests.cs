#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using AioTieba4DotNet.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Infrastructure;

[TestClass]
public sealed class TestArchitectureContractTests
{
    [TestMethod]
    public void SequencingManifest_UsesExpectedBusinessOrder()
    {
        var manifest = TestSequencingManifest.LoadDefault();

        manifest.ValidateExpectedBusinessOrder();

        CollectionAssert.AreEqual(
            TestSequencingManifest.ExpectedBusinessOrder.ToArray(),
            manifest.GetStageNames().ToArray());
    }

    [TestMethod]
    public void LocalEntrypoints_Exist_ForAllLanes_AndSequenceDryRun()
    {
        var scriptsDirectory = RepositoryPaths.GetScriptsDirectory();

        Assert.IsTrue(File.Exists(Path.Combine(scriptsDirectory, "test-lane.ps1")));
        Assert.IsTrue(File.Exists(Path.Combine(scriptsDirectory, "test-lane.sh")));
        Assert.IsTrue(File.Exists(Path.Combine(scriptsDirectory, "verify-local.ps1")));
        Assert.IsTrue(File.Exists(Path.Combine(scriptsDirectory, "verify-local.sh")));
    }

    [TestMethod]
    public void LaneProjectFiles_DoNotCompile_LegacyMixedTestTree()
    {
        var projectFiles = new[]
        {
            RepositoryPaths.GetDeterministicTestProjectPath(),
            RepositoryPaths.GetIntegrationTestProjectPath(),
            RepositoryPaths.GetLiveTestProjectPath()
        };

        foreach (var projectFile in projectFiles)
        {
            var projectText = File.ReadAllText(projectFile);
            Assert.DoesNotContain("..\\AioTieba4DotNet.Tests\\", projectText);
        }
    }

    [TestMethod]
    public void HistoricalMixedTestTree_ContainsNoActiveSourceFiles()
    {
        var legacyDirectory = RepositoryPaths.GetLegacyMixedTestDirectory();
        var remainingSourceFiles = Directory.Exists(legacyDirectory)
            ? Directory.EnumerateFiles(legacyDirectory, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains("\\bin\\", StringComparison.OrdinalIgnoreCase))
                .Where(path => !path.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase))
                .Where(path => !path.Contains("\\TestResults\\", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : [];

        Assert.IsEmpty(remainingSourceFiles,
            $"Historical mixed test tree must not keep active source files:{Environment.NewLine}- "
            + string.Join(Environment.NewLine + "- ", remainingSourceFiles));
    }

    [TestMethod]
    public void LocalEntrypoints_RunOrderedLanes_UsingManifestBackedStageFilters()
    {
        var scriptsDirectory = RepositoryPaths.GetScriptsDirectory();
        var ps1Text = File.ReadAllText(Path.Combine(scriptsDirectory, "test-lane.ps1"));
        var shText = File.ReadAllText(Path.Combine(scriptsDirectory, "test-lane.sh"));

        foreach (var scriptText in new[] { ps1Text, shText })
        {
            Assert.Contains("test-sequencing.manifest.json", scriptText);
            Assert.Contains("TestCategory=Integration&TestCategory=", scriptText);
            Assert.Contains("TestCategory=Live&TestCategory=", scriptText);
            Assert.DoesNotContain("TestCategory=Live&TestCategory=Cleanup", scriptText);
            Assert.Contains("CollectCoverage=false", scriptText);
            Assert.Contains("cleanup compensations / recorded object ledger", scriptText);
        }
    }

    [TestMethod]
    public void DeterministicLane_RetainsFinalCoverageCollection()
    {
        var scriptsDirectory = RepositoryPaths.GetScriptsDirectory();
        var ps1Text = File.ReadAllText(Path.Combine(scriptsDirectory, "test-lane.ps1"));
        var shText = File.ReadAllText(Path.Combine(scriptsDirectory, "test-lane.sh"));

        foreach (var scriptText in new[] { ps1Text, shText })
        {
            Assert.Contains("deterministic", scriptText);
            Assert.Contains("CollectCoverage=true", scriptText);
        }
    }

    [TestMethod]
    public void SequencingManifest_LaneMembership_CoversTaggedIntegrationAndLiveStages()
    {
        var manifest = TestSequencingManifest.LoadDefault();

        AssertTaggedStagesBelongToLane(
            "AioTieba4DotNet.Tests.Integration",
            TestCategoryNames.Integration,
            manifest.GetStagesForLane(TestLaneOrchestrator.IntegrationLane).Select(static stage => stage.Name));
        AssertTaggedStagesBelongToLane(
            "AioTieba4DotNet.Tests.Live",
            TestCategoryNames.Live,
            manifest.GetStagesForLane(TestLaneOrchestrator.LiveLane).Select(static stage => stage.Name));
    }

    [TestMethod]
    public void SequencingManifest_LiveLane_ExactlyMatchesTaggedLiveStages()
    {
        var manifest = TestSequencingManifest.LoadDefault();

        AssertTaggedStagesExactlyMatchLane(
            "AioTieba4DotNet.Tests.Live",
            TestCategoryNames.Live,
            manifest.GetStagesForLane(TestLaneOrchestrator.LiveLane)
                .Where(static stage => stage.Name != TestCategoryNames.Cleanup)
                .Select(static stage => stage.Name));
    }

    [TestMethod]
    public void GitHubWorkflows_DoNotRunTests_OrSecretBackedLanes()
    {
        var workflowDirectory = RepositoryPaths.GetWorkflowDirectory();
        var workflowTexts = Directory.EnumerateFiles(workflowDirectory, "*.yml", SearchOption.TopDirectoryOnly)
            .Select(File.ReadAllText)
            .ToArray();

        foreach (var workflowText in workflowTexts)
        {
            Assert.DoesNotContain("dotnet test", workflowText);
            Assert.DoesNotContain("AioTieba4DotNet.Tests/AioTieba4DotNet.Tests.csproj", workflowText);
            Assert.DoesNotContain("AioTieba4DotNet.Tests.Deterministic", workflowText);
            Assert.DoesNotContain("AioTieba4DotNet.Tests.Integration", workflowText);
            Assert.DoesNotContain("AioTieba4DotNet.Tests.Live", workflowText);
            Assert.DoesNotContain("TIEBA_BDUSS", workflowText);
            Assert.DoesNotContain("TIEBA_STOKEN", workflowText);
        }
    }

    private static void AssertTaggedStagesBelongToLane(string projectDirectoryName, string laneCategory,
        IEnumerable<string> allowedStageNames)
    {
        var repositoryRoot = RepositoryPaths.FindRepositoryRoot();
        var projectDirectory = Path.Combine(repositoryRoot, projectDirectoryName);
        var allowedStages = allowedStageNames.ToHashSet(StringComparer.Ordinal);

        var taggedStages = Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains("\\bin\\", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains("\\TestResults\\", StringComparison.OrdinalIgnoreCase))
            .Select(File.ReadAllText)
            .Where(text => text.Contains($"TestCategory(TestCategoryNames.{laneCategory})", StringComparison.Ordinal))
            .SelectMany(text => TestSequencingManifest.ExpectedBusinessOrder.Where(stageName =>
                text.Contains($"TestCategory(TestCategoryNames.{stageName})", StringComparison.Ordinal)))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(stageName => stageName, StringComparer.Ordinal)
            .ToArray();

        foreach (var taggedStage in taggedStages)
        {
            Assert.IsTrue(allowedStages.Contains(taggedStage),
                $"Stage '{taggedStage}' is tagged in {projectDirectoryName} but missing from the '{laneCategory}' manifest lane.");
        }
    }

    private static void AssertTaggedStagesExactlyMatchLane(string projectDirectoryName, string laneCategory,
        IEnumerable<string> expectedStageNames)
    {
        var repositoryRoot = RepositoryPaths.FindRepositoryRoot();
        var projectDirectory = Path.Combine(repositoryRoot, projectDirectoryName);
        var expectedStages = expectedStageNames.ToHashSet(StringComparer.Ordinal);

        var taggedStages = Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains("\\bin\\", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains("\\TestResults\\", StringComparison.OrdinalIgnoreCase))
            .Select(File.ReadAllText)
            .Where(text => text.Contains($"TestCategory(TestCategoryNames.{laneCategory})", StringComparison.Ordinal))
            .SelectMany(text => TestSequencingManifest.ExpectedBusinessOrder.Where(stageName =>
                text.Contains($"TestCategory(TestCategoryNames.{stageName})", StringComparison.Ordinal)))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(stageName => stageName, StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(expectedStages.OrderBy(stageName => stageName, StringComparer.Ordinal).ToArray(), taggedStages);
    }
}
