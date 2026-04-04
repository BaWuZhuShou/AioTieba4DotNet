#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AioTieba4DotNet.Tests.Infrastructure.Contracts;
using AioTieba4DotNet.Tests.Infrastructure.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Online.Suite.Contracts;

[TestClass]
[TestCategory(OnlineTestContractCategories.Style)]
public sealed class OnlineStyleContractTests
{
    private const string DataTestMethodMarker = "[Data" + "TestMethod]";
    private const string StringAssertMarker = "String" + "Assert.";
    private const string ObjectArrayEnumerableMarker = "IEnumerable<object" + "[]>";
    private const string ObjectArrayCreationMarker = "new object" + "[]";
    private const string EmptyStringAreEqualMarker = "Assert.AreEqual(" + "string.Empty,";

    private static readonly Regex TestClassRegex = new(
        @"\[TestClass\][\s\S]*?public\s+sealed\s+class\s+(?<name>\w+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [TestMethod]
    public void MigratedOnlineTestFiles_UseTestsSuffixes_And_SealedTestClassNames()
    {
        var testDocuments = EnumerateTestDocuments().ToArray();

        Assert.IsNotEmpty(testDocuments, "Expected the migrated online family to contain discoverable MSTest classes.");

        foreach (var document in testDocuments)
        {
            var fileName = Path.GetFileName(document.FilePath);
            Assert.IsTrue(
                fileName.EndsWith("Tests.cs", StringComparison.Ordinal),
                $"Test file '{fileName}' must use the '*Tests.cs' suffix.");

            var match = TestClassRegex.Match(document.Content);
            Assert.IsTrue(match.Success, $"Test file '{fileName}' must declare a public sealed MSTest class.");
            Assert.AreEqual(
                Path.GetFileNameWithoutExtension(document.FilePath),
                match.Groups["name"].Value,
                $"Test class name in '{fileName}' should match the file name for discoverability.");
        }
    }

    [TestMethod]
    public void MigratedOnlineTestFiles_AvoidLegacyMSTestHelpers_And_DataDrivenShapes()
    {
        foreach (var document in EnumerateTestDocuments())
        {
            Assert.DoesNotContain(
                DataTestMethodMarker,
                document.Content,
                $"Test file '{document.RelativePath}' should use the standard MSTest method attribute for discoverable tests.");
            Assert.DoesNotContain(
                StringAssertMarker,
                document.Content,
                $"Test file '{document.RelativePath}' should prefer core Assert APIs over legacy specialized string helpers when equivalent checks exist.");
            Assert.DoesNotContain(
                ObjectArrayEnumerableMarker,
                document.Content,
                $"Test file '{document.RelativePath}' should avoid loosely typed array-based DynamicData shapes.");
            Assert.DoesNotContain(
                ObjectArrayCreationMarker,
                document.Content,
                $"Test file '{document.RelativePath}' should avoid loosely typed array-based DynamicData cases.");
            Assert.DoesNotContain(
                EmptyStringAreEqualMarker,
                document.Content,
                $"Test file '{document.RelativePath}' should prefer the dedicated empty-string assertion for clarity.");
        }
    }

    private static IEnumerable<TestDocument> EnumerateTestDocuments()
    {
        foreach (var projectName in OnlineTestProjectTopology.ProjectNames)
        {
            var projectDirectory = RepositoryPaths.GetProjectDirectory(projectName);
            foreach (var filePath in Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories))
            {
                if (IsBuildArtifact(filePath))
                    continue;

                var content = File.ReadAllText(filePath);
                if (!content.Contains("[TestClass]", StringComparison.Ordinal))
                    continue;

                yield return new TestDocument(filePath, Path.GetRelativePath(RepositoryPaths.FindRepositoryRoot(), filePath), content);
            }
        }
    }

    private static bool IsBuildArtifact(string filePath)
    {
        return filePath.Contains("\\bin\\", StringComparison.OrdinalIgnoreCase)
               || filePath.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase)
               || filePath.Contains("\\TestResults\\", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record TestDocument(string FilePath, string RelativePath, string Content);
}
