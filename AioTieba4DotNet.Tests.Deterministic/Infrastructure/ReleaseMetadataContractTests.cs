#nullable enable
using System.IO;
using System.Xml.Linq;
using AioTieba4DotNet.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Infrastructure;

[TestClass]
public sealed class ReleaseMetadataContractTests
{
    [TestMethod]
    public void PublishWorkflow_UsesNet10Only_And_V3ReleaseTags()
    {
        var repositoryRoot = RepositoryPaths.FindRepositoryRoot();
        var workflowPath = Path.Combine(repositoryRoot, ".github", "workflows", "publish.yml");
        var workflowText = File.ReadAllText(workflowPath);

        Assert.Contains("dotnet-version: 10.x", workflowText);
        Assert.DoesNotContain("8.x", workflowText);
        Assert.DoesNotContain("9.x", workflowText);
        Assert.Contains("^3\\.0\\.0(-(preview|rc)\\.[0-9]+)?$", workflowText);
    }

    [TestMethod]
    public void PackageProject_UsesV3Identity_And_DoesNotPackDuringBuild()
    {
        var repositoryRoot = RepositoryPaths.FindRepositoryRoot();
        var projectPath = Path.Combine(repositoryRoot, "AioTieba4DotNet", "AioTieba4DotNet.csproj");
        var project = XDocument.Load(projectPath);
        var propertyGroup = project.Root!.Element("PropertyGroup");

        Assert.IsNotNull(propertyGroup);
        Assert.AreEqual("AioTieba4DotNet", propertyGroup.Element("PackageId")?.Value);
        Assert.AreEqual("3.0.0", propertyGroup.Element("VersionPrefix")?.Value);
        Assert.AreEqual("false", propertyGroup.Element("GeneratePackageOnBuild")?.Value);
        Assert.IsNull(propertyGroup.Element("Id"), "Legacy package Id metadata should not remain in the v3 package project.");
        Assert.IsNull(propertyGroup.Element("Version"), "The project should rely on VersionPrefix plus release-tag overrides instead of a stale fixed Version value.");
    }
}
