#nullable enable
using System.IO;
using AioTieba4DotNet.Tests.Infrastructure.Contracts;
using AioTieba4DotNet.Tests.Infrastructure.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Online.Suite.Contracts;

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

        foreach (var projectName in OnlineTestProjectTopology.ProjectNames)
            Assert.Contains(projectName, propsText);

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
        var infrastructureReference =
            $"..\\{OnlineTestProjectTopology.Infrastructure}\\{OnlineTestProjectTopology.Infrastructure}.csproj";

        foreach (var projectName in OnlineTestProjectTopology.ProjectNames)
        {
            var snapshot = ProjectFileSnapshot.Load(projectName);

            foreach (var propertyName in OnlineTestProjectBaseline.CentralizedPropertyNames)
                Assert.IsFalse(
                    snapshot.HasProperty(propertyName),
                    $"Project '{projectName}' should inherit '{propertyName}' from the repo-level online test baseline.");

            Assert.IsEmpty(snapshot.PackageReferences, $"Project '{projectName}' should not repeat repo-level MSTest package references.");
            Assert.IsEmpty(snapshot.CompileIncludes, $"Project '{projectName}' should inherit compile globs from Directory.Build.targets.");
            Assert.IsEmpty(snapshot.CompileRemovals, $"Project '{projectName}' should inherit compile removals from Directory.Build.targets.");

            if (projectName == OnlineTestProjectTopology.Infrastructure)
            {
                Assert.IsEmpty(snapshot.ProjectReferences, "The infrastructure shell should stay dependency-free at the project-file level.");
                continue;
            }

            if (projectName == OnlineTestProjectTopology.Suite)
            {
                CollectionAssert.AreEqual(new[] { infrastructureReference }, snapshot.ProjectReferences);
                continue;
            }

            CollectionAssert.AreEqual(new[] { productReference, infrastructureReference }, snapshot.ProjectReferences);
        }
    }

}
