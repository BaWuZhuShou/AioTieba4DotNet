using System.Diagnostics;
using System.Runtime.InteropServices;

// 1. 自动定位项目根目录
var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
DirectoryInfo? projectRootDir = currentDir;

// 向上寻找包含 .sln 的目录作为项目根目录
while (projectRootDir != null && !File.Exists(Path.Combine(projectRootDir.FullName, "AioTieba4DotNet.sln")))
{
    projectRootDir = projectRootDir.Parent;
}

if (projectRootDir == null)
{
    Console.WriteLine("❌ 错误: 无法找到项目根目录 (未找到 AioTieba4DotNet.sln)。");
    return 1;
}

string projectRoot = projectRootDir.FullName;
string apiDir = Path.Combine(projectRoot, "AioTieba4DotNet", "Api");
string baseProtobufDir = Path.Combine(apiDir, "Protobuf");

Console.WriteLine($"🚀 开始生成 Proto 代码...");
Console.WriteLine($"📂 项目根目录: {projectRoot}");
Console.WriteLine($"📂 公共 Protobuf 目录: {baseProtobufDir}");

// 2. 寻找 protoc 可执行文件
string? protocPath = FindProtocPath();

if (protocPath == null)
{
    protocPath = "protoc"; // 尝试使用系统 PATH
    Console.WriteLine("⚠️  未在工具目录找到 protoc，将尝试使用系统 PATH 中的 protoc。");
}
else
{
    Console.WriteLine($"🛠️  使用内置 protoc: {protocPath}");
}

// 3. 递归搜索所有 .proto 文件
if (!Directory.Exists(apiDir))
{
    Console.WriteLine($"❌ 错误: 找不到 API 目录 {apiDir}");
    return 1;
}

var protoFiles = Directory.GetFiles(apiDir, "*.proto", SearchOption.AllDirectories);

if (protoFiles.Length == 0)
{
    Console.WriteLine("ℹ️  未找到任何 .proto 文件。");
    return 0;
}

int successCount = 0;
int failCount = 0;

var stopwatch = Stopwatch.StartNew();

foreach (var protoFile in protoFiles)
{
    var relativePath = Path.GetRelativePath(projectRoot, protoFile);
    
    if (GenerateCSharp(protoFile))
    {
        Console.WriteLine($"✅ 已处理: {relativePath}");
        successCount++;
    }
    else
    {
        Console.WriteLine($"❌ 失败: {relativePath}");
        failCount++;
    }
}

stopwatch.Stop();
Console.WriteLine("\n========================================");
Console.WriteLine($"🏁 生成完成！");
Console.WriteLine($"⏱️  耗时: {stopwatch.Elapsed.TotalSeconds:F2}s");
Console.WriteLine($"✅ 成功: {successCount}");
if (failCount > 0)
{
    Console.WriteLine($"❌ 失败: {failCount}");
}
Console.WriteLine("========================================\n");

return failCount == 0 ? 0 : 1;

bool GenerateCSharp(string protoFile)
{
    var directory = Path.GetDirectoryName(protoFile)!;
    
    // 构造命令参数
    // 注意：这里保持了原始脚本的逻辑，将包含路径设为文件所在目录以及公共目录
    var args = new[]
    {
        "--csharp_opt=serializable",
        $"--csharp_out=\"{directory}\"",
        $"--proto_path=\"{directory}\"",
        $"-I \"{baseProtobufDir}\"",
        $"\"{protoFile}\""
    };

    var startInfo = new ProcessStartInfo
    {
        FileName = protocPath!,
        Arguments = string.Join(" ", args),
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    try
    {
        using var process = Process.Start(startInfo);
        if (process == null) return false;

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            if (!string.IsNullOrEmpty(output)) Console.WriteLine("输出: " + output);
            if (!string.IsNullOrEmpty(error)) Console.WriteLine("错误: " + error);
            return false;
        }
        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"异常: {ex.Message}");
        return false;
    }
}

string? FindProtocPath()
{
    var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "protoc.exe" : "protoc";
    
    // 检查程序运行目录（我们在 csproj 中配置了复制 protoc 到此处）
    var localPath = Path.Combine(AppContext.BaseDirectory, exeName);
    if (File.Exists(localPath)) return localPath;

    return null;
}
