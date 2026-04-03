namespace ProtoGenerator;

internal static class ProtoGenerationPlanner
{
    private const string SolutionFileName = "AioTieba4DotNet.sln";

    public static string? FindProjectRoot(string baseDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(baseDirectory);

        for (var directory = new DirectoryInfo(baseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Join(directory.FullName, SolutionFileName)))
                return directory.FullName;

        return null;
    }

    public static IReadOnlyList<ProtoGenerationTarget> DiscoverTargets(string projectRoot, string apiDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(projectRoot);
        ArgumentException.ThrowIfNullOrEmpty(apiDirectory);

        if (!Directory.Exists(apiDirectory)) throw new DirectoryNotFoundException($"找不到 API 目录 {apiDirectory}");

        return Directory
            .EnumerateFiles(apiDirectory, "*.proto", SearchOption.AllDirectories)
            .Select(path => Path.GetFullPath(path))
            .Select(path => new ProtoGenerationTarget(
                NormalizeRelativePath(Path.GetRelativePath(projectRoot, path)),
                path,
                Path.GetDirectoryName(path) ?? throw new InvalidOperationException($"无法确定输出目录: {path}")))
            .OrderBy(target => target.ProjectRelativePath, StringComparer.Ordinal)
            .ToArray();
    }

    internal static string NormalizeRelativePath(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        return path.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
    }
}
