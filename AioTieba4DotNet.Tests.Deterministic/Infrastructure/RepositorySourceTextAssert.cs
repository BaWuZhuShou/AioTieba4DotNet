using System.IO;
using System.Linq;
using AioTieba4DotNet.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Infrastructure;

internal static class RepositorySourceTextAssert
{
    public static string ReadRepositoryFiles(params string[] relativePaths)
    {
        var repositoryRoot = RepositoryPaths.FindRepositoryRoot();
        return string.Join(
            System.Environment.NewLine,
            relativePaths.Select(relativePath =>
            {
                var fullPath = Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
                Assert.IsTrue(File.Exists(fullPath), $"Expected repository file: {relativePath}");
                return File.ReadAllText(fullPath);
            }));
    }

    public static void ContainsAll(string text, params string[] expectedValues)
    {
        foreach (var expectedValue in expectedValues)
        {
            Assert.Contains(expectedValue, text);
        }
    }

    public static void DoesNotContainAny(string text, params string[] unexpectedValues)
    {
        foreach (var unexpectedValue in unexpectedValues)
        {
            Assert.DoesNotContain(unexpectedValue, text);
        }
    }
}
