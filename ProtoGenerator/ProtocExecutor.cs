using System.Diagnostics;

namespace ProtoGenerator;

internal interface IProtocExecutor
{
    Task<ProtoExecutionResult> GenerateAsync(
        string protocPath,
        string baseProtobufDirectory,
        ProtoGenerationTarget target,
        CancellationToken cancellationToken);
}

internal readonly record struct ProtoExecutionResult(
    bool Succeeded,
    string StandardOutput,
    string StandardError,
    string? FailureReason);

internal sealed class ProtocExecutor : IProtocExecutor
{
    private const string CSharpOptions = "serializable,internal_access";

    public static string? FindBundledProtocPath(string baseDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(baseDirectory);

        foreach (var executableName in new[] { "protoc.exe", "protoc" })
        {
            var localPath = Path.Combine(baseDirectory, executableName);
            if (File.Exists(localPath))
                return localPath;
        }

        return null;
    }

    internal static ProcessStartInfo CreateStartInfo(
        string protocPath,
        string baseProtobufDirectory,
        ProtoGenerationTarget target)
    {
        ArgumentException.ThrowIfNullOrEmpty(protocPath);
        ArgumentException.ThrowIfNullOrEmpty(baseProtobufDirectory);
        ArgumentNullException.ThrowIfNull(target);

        var startInfo = new ProcessStartInfo
        {
            FileName = protocPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add($"--csharp_opt={CSharpOptions}");
        startInfo.ArgumentList.Add($"--csharp_out={target.OutputDirectory}");
        startInfo.ArgumentList.Add($"--proto_path={target.ProtoDirectory}");
        startInfo.ArgumentList.Add($"--proto_path={baseProtobufDirectory}");
        startInfo.ArgumentList.Add(target.ProtoFilePath);

        return startInfo;
    }

    public async Task<ProtoExecutionResult> GenerateAsync(
        string protocPath,
        string baseProtobufDirectory,
        ProtoGenerationTarget target,
        CancellationToken cancellationToken)
    {
        var startInfo = CreateStartInfo(protocPath, baseProtobufDirectory, target);

        try
        {
            using var process = new Process { StartInfo = startInfo };
            if (!process.Start())
            {
                return new ProtoExecutionResult(false, string.Empty, string.Empty, "无法启动 protoc 进程。");
            }

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var error = await errorTask;

            return process.ExitCode == 0
                ? new ProtoExecutionResult(true, output, error, null)
                : new ProtoExecutionResult(false, output, error, $"protoc exited with code {process.ExitCode}.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new ProtoExecutionResult(false, string.Empty, string.Empty, ex.Message);
        }
    }
}
