using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoGenerator;

namespace AioTieba4DotNet.Tests.ProtoGenerator;

[TestClass]
public sealed class ProtoGeneratorAppTests
{
    [TestMethod]
    public void Find_project_root_returns_nearest_solution_ancestor()
    {
        using var tempDirectory = TemporaryDirectory.Create();
        var nestedDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.Path, "artifacts", "bin", "Debug", "net10.0"));
        File.WriteAllText(Path.Combine(tempDirectory.Path, "AioTieba4DotNet.sln"), string.Empty);

        var projectRoot = ProtoGenerationPlanner.FindProjectRoot(nestedDirectory.FullName);

        Assert.AreEqual(tempDirectory.Path, projectRoot);
    }

    [TestMethod]
    public void Discover_targets_orders_relative_paths_deterministically()
    {
        using var tempDirectory = TemporaryDirectory.Create();
        var apiDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.Path, "AioTieba4DotNet", "Api"));

        CreateProtoFile(apiDirectory.FullName, "z-last.proto");
        CreateProtoFile(apiDirectory.FullName, Path.Combine("Alpha", "b-middle.proto"));
        CreateProtoFile(apiDirectory.FullName, Path.Combine("Alpha", "a-first.proto"));

        var targets = ProtoGenerationPlanner.DiscoverTargets(tempDirectory.Path, apiDirectory.FullName);

        CollectionAssert.AreEqual(
            new[]
            {
                "AioTieba4DotNet/Api/Alpha/a-first.proto",
                "AioTieba4DotNet/Api/Alpha/b-middle.proto",
                "AioTieba4DotNet/Api/z-last.proto"
            },
            targets.Select(target => target.ProjectRelativePath).ToArray());
    }

    [TestMethod]
    public void Create_start_info_uses_internal_access_and_expected_proto_boundaries()
    {
        var target = new ProtoGenerationTarget(
            "AioTieba4DotNet/Api/GetThreads/Protobuf/FrsPageReqIdl.proto",
            Path.Combine("E:\\repo", "AioTieba4DotNet", "Api", "GetThreads", "FrsPageReqIdl.proto"),
            Path.Combine("E:\\repo", "AioTieba4DotNet", "Api", "GetThreads", "Protobuf"));
        var sharedDirectory = Path.Combine("E:\\repo", "AioTieba4DotNet", "Api", "Protobuf");

        var startInfo = ProtocExecutor.CreateStartInfo("protoc", sharedDirectory, target);

        CollectionAssert.AreEqual(
            new[]
            {
                "--csharp_opt=serializable,internal_access",
                $"--csharp_out={target.OutputDirectory}",
                $"--proto_path={target.ProtoDirectory}",
                $"--proto_path={sharedDirectory}",
                target.ProtoFilePath
            },
            startInfo.ArgumentList.ToArray());
    }

    [TestMethod]
    public void Find_bundled_protoc_path_returns_null_when_missing()
    {
        using var tempDirectory = TemporaryDirectory.Create();

        var bundledProtocPath = ProtocExecutor.FindBundledProtocPath(tempDirectory.Path);

        Assert.IsNull(bundledProtocPath);
    }

    [TestMethod]
    public void Find_bundled_protoc_path_returns_local_path_when_present()
    {
        using var tempDirectory = TemporaryDirectory.Create();
        var executableName = GetBundledProtocFileName();
        var bundledProtocPath = Path.Combine(tempDirectory.Path, executableName);
        File.WriteAllText(bundledProtocPath, string.Empty);

        var resolvedPath = ProtocExecutor.FindBundledProtocPath(tempDirectory.Path);

        Assert.AreEqual(bundledProtocPath, resolvedPath);
    }

    [TestMethod]
    public void Proto_generation_target_throws_when_proto_directory_cannot_be_determined()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => new ProtoGenerationTarget(
            "Simple.proto",
            string.Empty,
            Path.Combine("out", "protobuf")));
    }

    [TestMethod]
    public async Task Generate_async_invokes_real_protoc_for_simple_proto()
    {
        using var tempDirectory = TemporaryDirectory.Create();
        var protoDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.Path, "Protobuf"));
        var protoFilePath = Path.Combine(protoDirectory.FullName, "SimpleMessage.proto");
        File.WriteAllText(protoFilePath, "syntax = \"proto3\"; message SimpleMessage { string value = 1; }\n");

        var projectRoot = ProtoGenerationPlanner.FindProjectRoot(AppContext.BaseDirectory);
        Assert.IsNotNull(projectRoot);

        var protocPath = Path.Combine(projectRoot!, "ProtoGenerator", "bin", "Release", "net10.0", GetBundledProtocFileName());
        Assert.IsTrue(File.Exists(protocPath), $"Bundled protoc not found: {protocPath}");

        var target = new ProtoGenerationTarget("SimpleMessage.proto", protoFilePath, protoDirectory.FullName);
        var executor = new ProtocExecutor();

        var result = await executor.GenerateAsync(protocPath, protoDirectory.FullName, target, TestContext.CancellationToken);

        Assert.IsTrue(result.Succeeded, result.FailureReason ?? result.StandardError);
        var generatedFilePath = Path.Combine(protoDirectory.FullName, "SimpleMessage.cs");
        Assert.IsTrue(File.Exists(generatedFilePath));
        StringAssert.Contains(File.ReadAllText(generatedFilePath), "internal sealed partial class SimpleMessage");
    }

    [TestMethod]
    public async Task Run_async_reports_missing_project_root()
    {
        using var tempDirectory = TemporaryDirectory.Create();
        var output = new StringBuilder();
        using var writer = new StringWriter(output);

        var exitCode = await ProtoGeneratorApp.RunAsync(tempDirectory.Path, writer, new FakeProtocExecutor(), TestContext.CancellationToken);

        Assert.AreEqual(1, exitCode);
        StringAssert.Contains(output.ToString(), "无法找到项目根目录");
    }

    [TestMethod]
    public async Task Run_async_public_overload_reports_missing_project_root()
    {
        using var tempDirectory = TemporaryDirectory.Create();
        var output = new StringBuilder();
        using var writer = new StringWriter(output);

        var exitCode = await ProtoGeneratorApp.RunAsync(tempDirectory.Path, writer, TestContext.CancellationToken);

        Assert.AreEqual(1, exitCode);
        StringAssert.Contains(output.ToString(), "无法找到项目根目录");
    }

    [TestMethod]
    public async Task Run_async_reports_missing_api_directory()
    {
        using var tempDirectory = TemporaryDirectory.Create();
        File.WriteAllText(Path.Combine(tempDirectory.Path, "AioTieba4DotNet.sln"), string.Empty);

        var output = new StringBuilder();
        using var writer = new StringWriter(output);

        var exitCode = await ProtoGeneratorApp.RunAsync(tempDirectory.Path, writer, new FakeProtocExecutor(), TestContext.CancellationToken);

        Assert.AreEqual(1, exitCode);
        StringAssert.Contains(output.ToString(), "找不到 API 目录");
    }

    [TestMethod]
    public async Task Run_async_falls_back_to_path_when_bundled_protoc_is_missing()
    {
        using var tempDirectory = TemporaryDirectory.Create();
        File.WriteAllText(Path.Combine(tempDirectory.Path, "AioTieba4DotNet.sln"), string.Empty);

        var apiDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.Path, "AioTieba4DotNet", "Api"));
        CreateProtoFile(apiDirectory.FullName, "sample.proto");

        var baseDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.Path, "ProtoGenerator", "bin", "Release", "net10.0"));
        var executor = new FakeProtocExecutor();
        var output = new StringBuilder();
        using var writer = new StringWriter(output);

        var exitCode = await ProtoGeneratorApp.RunAsync(baseDirectory.FullName, writer, executor, TestContext.CancellationToken);

        Assert.AreEqual(0, exitCode);
        Assert.AreEqual("protoc", executor.ReceivedProtocPath);
        StringAssert.Contains(output.ToString(), "系统 PATH 中的 protoc");
    }

    [TestMethod]
    public async Task Run_async_uses_sorted_targets_and_returns_non_zero_when_any_generation_fails()
    {
        using var tempDirectory = TemporaryDirectory.Create();
        File.WriteAllText(Path.Combine(tempDirectory.Path, "AioTieba4DotNet.sln"), string.Empty);

        var apiDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.Path, "AioTieba4DotNet", "Api"));
        CreateProtoFile(apiDirectory.FullName, "z-last.proto");
        CreateProtoFile(apiDirectory.FullName, Path.Combine("Alpha", "a-first.proto"));
        CreateProtoFile(apiDirectory.FullName, Path.Combine("Alpha", "b-middle.proto"));

        var baseDirectory = Directory.CreateDirectory(Path.Combine(tempDirectory.Path, "ProtoGenerator", "bin", "Release", "net10.0"));
        File.WriteAllText(Path.Combine(baseDirectory.FullName, GetBundledProtocFileName()), string.Empty);

        var executor = new FakeProtocExecutor
        {
            ResultFactory = target => target.ProjectRelativePath.EndsWith("b-middle.proto", StringComparison.Ordinal)
                ? new ProtoExecutionResult(false, string.Empty, "bad proto", "synthetic failure")
                : new ProtoExecutionResult(true, string.Empty, string.Empty, null)
        };

        var output = new StringBuilder();
        using var writer = new StringWriter(output);

        var exitCode = await ProtoGeneratorApp.RunAsync(baseDirectory.FullName, writer, executor, TestContext.CancellationToken);

        Assert.AreEqual(1, exitCode);
        CollectionAssert.AreEqual(
            new[]
            {
                "AioTieba4DotNet/Api/Alpha/a-first.proto",
                "AioTieba4DotNet/Api/Alpha/b-middle.proto",
                "AioTieba4DotNet/Api/z-last.proto"
            },
            executor.ProcessedTargets.ToArray());
        StringAssert.Contains(output.ToString(), "🛠️  使用内置 protoc");
        StringAssert.Contains(output.ToString(), "❌ 失败: AioTieba4DotNet/Api/Alpha/b-middle.proto");
    }

    [TestMethod]
    public async Task Run_async_returns_zero_when_no_proto_files_exist()
    {
        using var tempDirectory = TemporaryDirectory.Create();
        File.WriteAllText(Path.Combine(tempDirectory.Path, "AioTieba4DotNet.sln"), string.Empty);
        _ = Directory.CreateDirectory(Path.Combine(tempDirectory.Path, "AioTieba4DotNet", "Api"));

        var output = new StringBuilder();
        using var writer = new StringWriter(output);

        var exitCode = await ProtoGeneratorApp.RunAsync(tempDirectory.Path, writer, new FakeProtocExecutor(), TestContext.CancellationToken);

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output.ToString(), "未找到任何 .proto 文件");
    }

    public TestContext TestContext { get; set; }

    private static void CreateProtoFile(string apiDirectory, string relativePath)
    {
        var fullPath = Path.Combine(apiDirectory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, "syntax = \"proto3\"; message Sample {}\n");
    }

    private static string GetBundledProtocFileName()
    {
        return OperatingSystem.IsWindows() ? "protoc.exe" : "protoc";
    }

    private sealed class FakeProtocExecutor : IProtocExecutor
    {
        public Func<ProtoGenerationTarget, ProtoExecutionResult> ResultFactory { get; init; } =
            _ => new ProtoExecutionResult(true, string.Empty, string.Empty, null);

        public List<string> ProcessedTargets { get; } = [];
        public string ReceivedProtocPath { get; private set; } = string.Empty;

        public Task<ProtoExecutionResult> GenerateAsync(
            string protocPath,
            string baseProtobufDirectory,
            ProtoGenerationTarget target,
            CancellationToken cancellationToken)
        {
            ReceivedProtocPath = protocPath;
            ProcessedTargets.Add(target.ProjectRelativePath);
            return Task.FromResult(ResultFactory(target));
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        private TemporaryDirectory(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TemporaryDirectory Create()
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            return new TemporaryDirectory(path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
