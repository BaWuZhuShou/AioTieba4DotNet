namespace ProtoGenerator;

internal sealed record ProtoGenerationTarget(string ProjectRelativePath, string ProtoFilePath, string OutputDirectory)
{
    public string ProtoDirectory { get; } = Path.GetDirectoryName(ProtoFilePath)
                                            ?? throw new InvalidOperationException($"无法确定 proto 文件目录: {ProtoFilePath}");
}
