using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace ProtoGenerator;

internal static class ProtoGeneratorApp
{
    public static Task<int> RunAsync(string baseDirectory, TextWriter output, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(output);
        return RunAsync(baseDirectory, output, new ProtocExecutor(), cancellationToken);
    }

    [SuppressMessage("Critical Code Smell", "S3776:Refactor this method to reduce its Cognitive Complexity",
        Justification = "The generator run method intentionally sequences discovery, validation, execution, and summary output in a single orchestration flow for reproducible command behavior.")]
    internal static async Task<int> RunAsync(
        string baseDirectory,
        TextWriter output,
        IProtocExecutor executor,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(baseDirectory);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(executor);

        var projectRoot = ProtoGenerationPlanner.FindProjectRoot(baseDirectory);
        if (projectRoot is null)
        {
            await output.WriteLineAsync("❌ 错误: 无法找到项目根目录 (未找到 AioTieba4DotNet.sln)。");
            return 1;
        }

        var apiDirectory = Path.Join(projectRoot, "AioTieba4DotNet", "Api");
        var baseProtobufDirectory = Path.Join(apiDirectory, "Protobuf");

        await output.WriteLineAsync("🚀 开始生成 Proto 代码...");
        await output.WriteLineAsync($"📂 项目根目录: {projectRoot}");
        await output.WriteLineAsync($"📂 公共 Protobuf 目录: {baseProtobufDirectory}");

        var protocPath = ProtocExecutor.FindBundledProtocPath(baseDirectory);
        if (protocPath is null)
        {
            protocPath = "protoc";
            await output.WriteLineAsync("⚠️  未在工具目录找到 protoc，将尝试使用系统 PATH 中的 protoc。");
        }
        else
        {
            await output.WriteLineAsync($"🛠️  使用内置 protoc: {protocPath}");
        }

        IReadOnlyList<ProtoGenerationTarget> targets;

        try
        {
            targets = ProtoGenerationPlanner.DiscoverTargets(projectRoot, apiDirectory);
        }
        catch (DirectoryNotFoundException ex)
        {
            await output.WriteLineAsync($"❌ 错误: {ex.Message}");
            return 1;
        }

        if (targets.Count == 0)
        {
            await output.WriteLineAsync("ℹ️  未找到任何 .proto 文件。");
            return 0;
        }

        var successCount = 0;
        var failCount = 0;
        var stopwatch = Stopwatch.StartNew();

        foreach (var target in targets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await executor.GenerateAsync(protocPath, baseProtobufDirectory, target, cancellationToken);
            if (result.Succeeded)
            {
                await output.WriteLineAsync($"✅ 已处理: {target.ProjectRelativePath}");
                successCount++;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(result.StandardOutput))
                await output.WriteLineAsync($"输出: {result.StandardOutput.TrimEnd()}");

            if (!string.IsNullOrWhiteSpace(result.StandardError))
                await output.WriteLineAsync($"错误: {result.StandardError.TrimEnd()}");

            if (!string.IsNullOrWhiteSpace(result.FailureReason))
                await output.WriteLineAsync($"异常: {result.FailureReason}");

            await output.WriteLineAsync($"❌ 失败: {target.ProjectRelativePath}");
            failCount++;
        }

        stopwatch.Stop();

        await output.WriteLineAsync();
        await output.WriteLineAsync("========================================");
        await output.WriteLineAsync("🏁 生成完成！");
        await output.WriteLineAsync($"⏱️  耗时: {stopwatch.Elapsed.TotalSeconds:F2}s");
        await output.WriteLineAsync($"✅ 成功: {successCount}");

        if (failCount > 0) await output.WriteLineAsync($"❌ 失败: {failCount}");

        await output.WriteLineAsync("========================================");
        await output.WriteLineAsync();

        return failCount == 0 ? 0 : 1;
    }
}
