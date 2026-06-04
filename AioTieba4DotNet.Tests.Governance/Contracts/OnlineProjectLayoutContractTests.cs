#nullable enable
using System.IO;
using AioTieba4DotNet.Tests.Platform.Contracts;
using AioTieba4DotNet.Tests.Platform.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Governance.Contracts;

[TestClass]
[TestCategory(OnlineTestContractCategories.ProjectLayout)]
public sealed class OnlineProjectLayoutContractTests
{
    [TestMethod]
    public void DirectoryBuildPropsClassifiesOnlyTheNewOnlineProjectFamily()
    {
        var propsText = File.ReadAllText(RepositoryPaths.GetDirectoryBuildPropsPath());

        Assert.Contains(OnlineTestProjectBaseline.OnlineTestProjectProperty, propsText);
        Assert.Contains(OnlineTestProjectBaseline.OnlineScenarioProjectProperty, propsText);
        Assert.Contains(OnlineTestProjectBaseline.OnlineContractProjectProperty, propsText);

        foreach (var projectName in OnlineTestProjectTopology.TestProjectNames)
            Assert.Contains(projectName, propsText);

        Assert.DoesNotContain(OnlineTestProjectTopology.Platform, propsText,
            "Directory.Build.props should classify only runnable/governance test projects, not the plain Platform support library.");

        foreach (var propertyName in OnlineTestProjectBaseline.CentralizedPropertyNames)
            Assert.Contains($"<{propertyName}>", propsText);
    }

    [TestMethod]
    public void RepoLevelBaselineCentralizesCommonOnlineTestPackagesAndCompileItems()
    {
        var targetsText = File.ReadAllText(RepositoryPaths.GetDirectoryBuildTargetsPath());
        var packagesText = File.ReadAllText(RepositoryPaths.GetDirectoryPackagesPropsPath());

        foreach (var packageReference in OnlineTestProjectBaseline.CentralizedPackageReferences)
        {
            Assert.Contains($"PackageReference Include=\"{packageReference}\"", targetsText);
            Assert.Contains($"PackageVersion Include=\"{packageReference}\"", packagesText);
        }

        Assert.Contains($"Compile Include=\"{OnlineTestProjectBaseline.SourceCompileInclude}\"", targetsText);
        Assert.Contains($"Compile Remove=\"{OnlineTestProjectBaseline.SourceCompileRemove}\"", targetsText);
    }

    [TestMethod]
    public void NewProjectFilesConsumeTheCentralizedBaselineWithoutLocalDuplicates()
    {
        var productReference = "..\\AioTieba4DotNet\\AioTieba4DotNet.csproj";
        var platformReference = "..\\AioTieba4DotNet.Tests.Platform\\AioTieba4DotNet.Tests.Platform.csproj";

        foreach (var projectName in OnlineTestProjectTopology.TestProjectNames)
        {
            var snapshot = ProjectFileSnapshot.Load(projectName);

            foreach (var propertyName in OnlineTestProjectBaseline.CentralizedPropertyNames)
                Assert.IsFalse(
                    snapshot.HasProperty(propertyName),
                    $"Project '{projectName}' should inherit '{propertyName}' from the repo-level online test baseline.");

            Assert.IsEmpty(snapshot.PackageReferences, $"Project '{projectName}' should not repeat repo-level MSTest package references.");
            Assert.IsEmpty(snapshot.CompileIncludes, $"Project '{projectName}' should inherit compile globs from Directory.Build.targets.");
            Assert.IsEmpty(snapshot.CompileRemovals, $"Project '{projectName}' should inherit compile removals from Directory.Build.targets.");

            if (projectName == OnlineTestProjectTopology.Governance)
            {
                CollectionAssert.AreEqual(new[] { productReference, platformReference }, snapshot.ProjectReferences);
                continue;
            }

            CollectionAssert.AreEqual(new[] { productReference, platformReference }, snapshot.ProjectReferences);
        }
    }

}
