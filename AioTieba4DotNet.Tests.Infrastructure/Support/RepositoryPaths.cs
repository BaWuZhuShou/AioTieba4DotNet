using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace AioTieba4DotNet.Tests.Infrastructure.Support;

[ExcludeFromCodeCoverage]
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

    public static string GetSolutionPath()
    {
        return Path.Combine(FindRepositoryRoot(), SolutionFileName);
    }

    public static string GetDirectoryBuildPropsPath()
    {
        return Path.Combine(FindRepositoryRoot(), "Directory.Build.props");
    }

    public static string GetDirectoryBuildTargetsPath()
    {
        return Path.Combine(FindRepositoryRoot(), "Directory.Build.targets");
    }

    public static string GetDirectoryPackagesPropsPath()
    {
        return Path.Combine(FindRepositoryRoot(), "Directory.Packages.props");
    }

    public static string GetProjectDirectory(string projectName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectName);
        return Path.Combine(FindRepositoryRoot(), projectName);
    }

    public static string GetProjectFilePath(string projectName)
    {
        return Path.Combine(GetProjectDirectory(projectName), $"{projectName}.csproj");
    }
}
