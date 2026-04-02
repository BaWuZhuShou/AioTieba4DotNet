#nullable enable
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AioTieba4DotNet.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Infrastructure;

[TestClass]
public sealed class LocalVerificationContractTests
{
    private static readonly string[] RequiredDocs =
    [
        "README.md",
        "docs/index.md",
        "docs/guide/getting-started.md",
        "docs/how-to/forums.md",
        "docs/how-to/threads.md",
        "docs/how-to/users.md",
        "docs/how-to/messages.md",
        "docs/reference/modules.md",
        "docs/guide/advanced.md",
        "docs/guide/troubleshooting.md",
        "docs/related/migration-v2-to-v3.md",
        "docs/related/release-notes-v3.md",
        "docs/related/parity-v3.md",
        "docs/archive/todo.md",
        "AGENTS.md",
        ".junie/guidelines.md"
    ];

    private static readonly string[] RequiredTemplateConfigs =
    [
        "AioTieba4DotNet.Testing/appsettings.test.json",
        "AioTieba4DotNet.Testing/appsettings.fixtures.example.json"
    ];

    private static readonly string[] LocalEntrypoints =
    [
        "scripts/verify-local.ps1",
        "scripts/verify-local.sh",
        "scripts/test-lane.ps1",
        "scripts/test-lane.sh"
    ];

    private static readonly EvidenceContract[] RequiredEvidence =
    [
        new("deterministic-tests-and-coverage", "local-verification", "18",
            ".sisyphus/evidence/local-deterministic-verification.md",
            "Record deterministic lane execution and coverage evidence outside GitHub Actions."),
        new("integration-lane", "local-verification", "19", ".sisyphus/evidence/local-integration-verification.md",
            "Record integration lane execution evidence outside GitHub Actions."),
        new("live-lane", "local-verification", "19", ".sisyphus/evidence/local-live-verification.md",
            "Record live lane execution and cleanup evidence outside GitHub Actions.")
    ];

    [TestMethod]
    public void Task11EvidenceFiles_Exist_And_AreNotEmpty()
    {
        var repositoryRoot = RepositoryPaths.FindRepositoryRoot();
        foreach (var relativePath in new[]
                 {
                     ".sisyphus/evidence/task-11-deterministic-lane.md", ".sisyphus/evidence/task-11-test-search.md"
                 })
        {
            var fullPath = Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.IsTrue(File.Exists(fullPath), $"Expected task evidence file: {relativePath}");
            Assert.IsTrue(new FileInfo(fullPath).Length > 0, $"Task evidence file must not be empty: {relativePath}");
        }
    }

    [TestMethod]
    public void GovernanceContractFiles_Exist_And_AreNotEmpty()
    {
        var repositoryRoot = RepositoryPaths.FindRepositoryRoot();
        var requiredFiles = RequiredDocs
            .Concat(LocalEntrypoints)
            .Concat(RequiredTemplateConfigs)
            .Concat([
                ".sisyphus/evidence/local-verification.manifest.json",
                ".sisyphus/evidence/local-verification.manifest.schema.json"
            ])
            .Concat(RequiredEvidence.Select(static evidence => evidence.Path))
            .ToArray();

        foreach (var relativePath in requiredFiles)
        {
            var fullPath = Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.IsTrue(File.Exists(fullPath), $"Expected governance contract file: {relativePath}");
            Assert.IsTrue(new FileInfo(fullPath).Length > 0,
                $"Governance contract file must not be empty: {relativePath}");
        }
    }

    [TestMethod]
    public void TrackedTestConfigTemplates_KeepCredentialFieldsBlank()
    {
        var repositoryRoot = RepositoryPaths.FindRepositoryRoot();

        AssertCredentialFieldBlank(repositoryRoot, "AioTieba4DotNet.Testing/appsettings.test.json", "TieBa", "BDUSS");
        AssertCredentialFieldBlank(repositoryRoot, "AioTieba4DotNet.Testing/appsettings.test.json", "TieBa", "STOKEN");
        AssertCredentialFieldBlank(repositoryRoot, "AioTieba4DotNet.Testing/appsettings.fixtures.example.json", "TieBa",
            "BDUSS");
        AssertCredentialFieldBlank(repositoryRoot, "AioTieba4DotNet.Testing/appsettings.fixtures.example.json", "TieBa",
            "STOKEN");
    }

    [TestMethod]
    public void LocalEvidenceFiles_AreCompletedRecords_NotPlaceholderStubs()
    {
        var repositoryRoot = RepositoryPaths.FindRepositoryRoot();

        AssertEvidenceRecord(
            repositoryRoot,
            ".sisyphus/evidence/local-deterministic-verification.md",
            new[]
            {
                "Command:", "pwsh -File \".\\scripts\\test-lane.ps1\" deterministic", "Coverage collected:",
                "Lane result: passed", "Result:"
            },
            new[] { "sequence-dry-run" });
        AssertEvidenceRecord(
            repositoryRoot,
            ".sisyphus/evidence/local-integration-verification.md",
            new[]
            {
                "Command:", "pwsh -File \".\\scripts\\test-lane.ps1\" integration", "Observed output:",
                "[integration]", "returned exit 0 in this environment", "real staged integration-lane execution",
                "Result:"
            },
            new[] { "sequence-dry-run" });
        AssertEvidenceRecord(
            repositoryRoot,
            ".sisyphus/evidence/local-live-verification.md",
            new[]
            {
                "Command:", "pwsh -File \".\\scripts\\test-lane.ps1\" sequence-dry-run -Stages ThreadRead,Cleanup",
                "Observed output:", "cleanup compensations / recorded object ledger",
                "does not claim that the credentialed live lane itself was executed here", "Result:"
            },
            Array.Empty<string>());
    }

    [TestMethod]
    public void CodeQlWorkflow_UsesNet10Only_Governance_Surface()
    {
        var repositoryRoot = RepositoryPaths.FindRepositoryRoot();
        var workflowText =
            File.ReadAllText(Path.Combine(repositoryRoot, ".github", "workflows", "codeql-analysis.yml"));

        Assert.Contains("branches: [ \"main\", \"master\" ]", workflowText);
        Assert.Contains("dotnet-version: 10.x", workflowText);
        Assert.DoesNotContain("\"v2\"", workflowText);
        Assert.DoesNotContain("8.x", workflowText);
        Assert.DoesNotContain("9.x", workflowText);
    }

    [TestMethod]
    public void LibraryAgentsGuide_DoesNotReferenceRemovedRequestPlumbingFiles()
    {
        var repositoryRoot = RepositoryPaths.FindRepositoryRoot();
        var guideText = File.ReadAllText(Path.Combine(repositoryRoot, "AioTieba4DotNet", "AGENTS.md"));

        Assert.DoesNotContain("Api/ProtoApiBase.cs", guideText);
        Assert.DoesNotContain("Api/ApiWsBase.cs", guideText);
        Assert.DoesNotContain("Api/ProtoApiWsBase.cs", guideText);
    }

    [TestMethod]
    public void LocalVerificationManifest_MatchesExpectedGovernanceContract()
    {
        var manifestPath = RepositoryPaths.GetLocalVerificationManifestPath();
        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        Assert.AreEqual("./local-verification.manifest.schema.json", root.GetProperty("$schema").GetString());
        Assert.AreEqual(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.AreEqual("v3", root.GetProperty("releaseLine").GetString());

        var generatedBy = root.GetProperty("generatedBy").GetString();
        CollectionAssert.Contains(new[] { "scripts/verify-local.ps1", "scripts/verify-local.sh" }, generatedBy!);

        CollectionAssert.AreEqual(RequiredDocs, ReadStringArray(root.GetProperty("requiredDocs")));
        CollectionAssert.AreEqual(LocalEntrypoints, ReadStringArray(root.GetProperty("localEntrypoints")));

        var policy = root.GetProperty("ciPolicy");
        Assert.IsFalse(policy.GetProperty("githubActionsRunsTests").GetBoolean());
        Assert.IsFalse(policy.GetProperty("githubActionsRunsSecretBackedLanes").GetBoolean());
        CollectionAssert.AreEqual(
            new[] { "restore", "build", "codegen", "packaging" },
            ReadStringArray(policy.GetProperty("releaseGateChecks")));

        CollectionAssert.AreEqual(
            RequiredEvidence,
            ReadEvidenceContracts(root.GetProperty("requiredEvidence")));
    }

    [TestMethod]
    public void VerifyLocalScripts_EmbedManifestGovernanceContract()
    {
        var scriptsDirectory = RepositoryPaths.GetScriptsDirectory();
        var scriptTexts = new[]
        {
            File.ReadAllText(Path.Combine(scriptsDirectory, "verify-local.ps1")),
            File.ReadAllText(Path.Combine(scriptsDirectory, "verify-local.sh"))
        };

        foreach (var scriptText in scriptTexts)
        {
            Assert.Contains("local-verification.manifest.json", scriptText);
            Assert.Contains("local-verification.manifest.schema.json", scriptText);
            Assert.Contains("docs/index.md", scriptText);
            Assert.Contains("docs/guide/getting-started.md", scriptText);
            Assert.Contains("docs/reference/modules.md", scriptText);
            Assert.Contains("docs/related/release-notes-v3.md", scriptText);
            Assert.Contains("docs/archive/todo.md", scriptText);
            Assert.Contains("local-deterministic-verification.md", scriptText);
            Assert.Contains("codeql-analysis.yml", scriptText);
            Assert.Contains("dotnet-version: 10.x", scriptText);
            Assert.Contains("githubActionsRunsTests", scriptText);
            Assert.Contains("AioTieba4DotNet.Testing/appsettings.test.json", scriptText);
            Assert.Contains("Update this file", scriptText);
            Assert.Contains("AioTieba4DotNet/AGENTS.md", scriptText);
            Assert.Contains("pnpm --dir docs install", scriptText);
            Assert.Contains("pnpm --dir docs run build", scriptText);
        }
    }

    [TestMethod]
    public void VerifyLocalValidateOnly_Fails_WhenTrackedTestConfigContainsCredentials()
    {
        using var fixture = CreateVerifyLocalFixture();
        File.WriteAllText(
            Path.Combine(fixture.RepositoryRoot, "AioTieba4DotNet.Testing", "appsettings.test.json"),
            "{" + Environment.NewLine
                + "  \"TieBa\": {" + Environment.NewLine
                + "    \"BDUSS\": \"unsafe\"," + Environment.NewLine
                + "    \"STOKEN\": \"unsafe\"" + Environment.NewLine
                + "  }" + Environment.NewLine
                + "}" + Environment.NewLine);

        var result = RunVerifyLocalValidateOnly(fixture.RepositoryRoot);

        Assert.AreNotEqual(0, result.ExitCode);
        StringAssert.Contains(result.Output,
            "Tracked credential template AioTieba4DotNet.Testing/appsettings.test.json must keep TieBa:BDUSS blank.");
    }

    [TestMethod]
    public void VerifyLocalValidateOnly_Fails_WhenLocalEvidenceIsPlaceholderStub()
    {
        using var fixture = CreateVerifyLocalFixture();
        File.WriteAllText(
            Path.Combine(fixture.RepositoryRoot, ".sisyphus", "evidence", "local-live-verification.md"),
            "# Local live verification evidence" + Environment.NewLine + Environment.NewLine
            + "- Update this file with created object IDs before a release-tag publish." + Environment.NewLine);

        var result = RunVerifyLocalValidateOnly(fixture.RepositoryRoot);

        Assert.AreNotEqual(0, result.ExitCode);
        StringAssert.Contains(result.Output,
            "Evidence record .sisyphus/evidence/local-live-verification.md must contain phrase: Command:");
        StringAssert.Contains(result.Output,
            "Evidence record .sisyphus/evidence/local-live-verification.md must not contain placeholder phrase: Update this file");
    }

    [TestMethod]
    public void VerifyLocalValidateOnly_Fails_WhenIntegrationEvidence_UsesDryRunPlaceholderShape()
    {
        using var fixture = CreateVerifyLocalFixture();
        File.WriteAllText(
            Path.Combine(fixture.RepositoryRoot, ".sisyphus", "evidence", "local-integration-verification.md"),
            "# Local integration verification evidence" + Environment.NewLine + Environment.NewLine
            + "## Recorded execution" + Environment.NewLine + Environment.NewLine
            + "Command:" + Environment.NewLine + Environment.NewLine
            + "```text" + Environment.NewLine
            + "pwsh -File \".\\scripts\\test-lane.ps1\" sequence-dry-run -Stages ThreadRead" + Environment.NewLine
            + "```" + Environment.NewLine + Environment.NewLine
            + "Observed output:" + Environment.NewLine + Environment.NewLine
            + "```text" + Environment.NewLine
            + "1. ThreadRead [integration, live]" + Environment.NewLine
            + "```" + Environment.NewLine + Environment.NewLine
            + "Result:" + Environment.NewLine + Environment.NewLine
            + "```text" + Environment.NewLine
            + "Passed." + Environment.NewLine
            + "```" + Environment.NewLine);

        var result = RunVerifyLocalValidateOnly(fixture.RepositoryRoot);

        Assert.AreNotEqual(0, result.ExitCode);
        StringAssert.Contains(result.Output,
            "Evidence record .sisyphus/evidence/local-integration-verification.md must contain phrase: pwsh -File \".\\scripts\\test-lane.ps1\" integration");
        StringAssert.Contains(result.Output,
            "Evidence record .sisyphus/evidence/local-integration-verification.md must not contain placeholder phrase: sequence-dry-run");
    }

    [TestMethod]
    public void VerifyLocalValidateOnly_Fails_WhenCodeQlWorkflow_StillAdvertisesLegacyReleaseLines()
    {
        using var fixture = CreateVerifyLocalFixture();
        File.WriteAllText(
            Path.Combine(fixture.RepositoryRoot, ".github", "workflows", "codeql-analysis.yml"),
            "name: \"CodeQL\"" + Environment.NewLine + Environment.NewLine
            + "on:" + Environment.NewLine
            + "    push:" + Environment.NewLine
            + "        branches: [ \"main\", \"master\", \"v2\" ]" + Environment.NewLine
            + "jobs:" + Environment.NewLine
            + "    analyze:" + Environment.NewLine
            + "        steps:" + Environment.NewLine
            + "            -   uses: actions/setup-dotnet@v5" + Environment.NewLine
            + "                with:" + Environment.NewLine
            + "                    dotnet-version: |" + Environment.NewLine
            + "                        8.x" + Environment.NewLine
            + "                        9.x" + Environment.NewLine
            + "                        10.x" + Environment.NewLine);

        var result = RunVerifyLocalValidateOnly(fixture.RepositoryRoot);

        Assert.AreNotEqual(0, result.ExitCode);
        StringAssert.Contains(result.Output,
            "Workflow governance file .github/workflows/codeql-analysis.yml must contain phrase: branches: [ \"main\", \"master\" ]");
        StringAssert.Contains(result.Output,
            "Workflow governance file .github/workflows/codeql-analysis.yml must not contain phrase: \"v2\"");
        StringAssert.Contains(result.Output,
            "Workflow governance file .github/workflows/codeql-analysis.yml must not contain phrase: 8.x");
    }

    [TestMethod]
    public void VerifyLocalValidateOnly_Fails_WhenLibraryGuideReferencesRemovedRequestPlumbing()
    {
        using var fixture = CreateVerifyLocalFixture();
        File.WriteAllText(
            Path.Combine(fixture.RepositoryRoot, "AioTieba4DotNet", "AGENTS.md"),
            File.ReadAllText(Path.Combine(fixture.RepositoryRoot, "AioTieba4DotNet", "AGENTS.md"))
            + Environment.NewLine
            + "Legacy request base reference: Api/ProtoApiBase.cs" + Environment.NewLine);

        var result = RunVerifyLocalValidateOnly(fixture.RepositoryRoot);

        Assert.AreNotEqual(0, result.ExitCode);
        StringAssert.Contains(result.Output,
            "Forbidden legacy spine reference 'ProtoApiBase' found in AioTieba4DotNet/AGENTS.md");
    }

    private static void AssertCredentialFieldBlank(string repositoryRoot, string relativePath, string section,
        string key)
    {
        var filePath = Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        using var document = JsonDocument.Parse(File.ReadAllText(filePath));
        var value = document.RootElement.GetProperty(section).GetProperty(key).GetString();
        Assert.IsTrue(string.IsNullOrWhiteSpace(value),
            $"Tracked template {relativePath} must keep {section}:{key} blank.");
    }

    private static void AssertEvidenceRecord(string repositoryRoot, string relativePath, string[] requiredPhrases,
        string[] forbiddenPhrases)
    {
        var filePath = Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var text = File.ReadAllText(filePath);

        Assert.DoesNotContain("Update this file", text);
        foreach (var phrase in requiredPhrases) Assert.Contains(phrase, text);

        foreach (var phrase in forbiddenPhrases) Assert.DoesNotContain(phrase, text);
    }

    private static VerifyLocalFixture CreateVerifyLocalFixture()
    {
        var repositoryRoot = RepositoryPaths.FindRepositoryRoot();
        var tempRoot = Path.Combine(Path.GetTempPath(), $"verify-local-fixture-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        CopyDirectory(Path.Combine(repositoryRoot, "scripts"), Path.Combine(tempRoot, "scripts"));
        CopyDirectory(Path.Combine(repositoryRoot, ".sisyphus", "evidence"),
            Path.Combine(tempRoot, ".sisyphus", "evidence"));
        CopyDirectory(Path.Combine(repositoryRoot, ".junie"), Path.Combine(tempRoot, ".junie"));
        CopyDirectory(Path.Combine(repositoryRoot, ".github", "workflows"),
            Path.Combine(tempRoot, ".github", "workflows"));
        CopyDirectory(Path.Combine(repositoryRoot, "docs"), Path.Combine(tempRoot, "docs"));
        CopyDirectory(Path.Combine(repositoryRoot, "AioTieba4DotNet"), Path.Combine(tempRoot, "AioTieba4DotNet"));
        CopyDirectory(Path.Combine(repositoryRoot, "AioTieba4DotNet.Testing"),
            Path.Combine(tempRoot, "AioTieba4DotNet.Testing"));
        File.Copy(Path.Combine(repositoryRoot, "README.md"), Path.Combine(tempRoot, "README.md"), true);
        File.Copy(Path.Combine(repositoryRoot, "AGENTS.md"), Path.Combine(tempRoot, "AGENTS.md"), true);

        return new VerifyLocalFixture(tempRoot);
    }

    private static VerifyLocalResult RunVerifyLocalValidateOnly(string repositoryRoot)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "pwsh",
                Arguments =
                    $"-NoProfile -File \"{Path.Combine(repositoryRoot, "scripts", "verify-local.ps1")}\" -ValidateOnly",
                WorkingDirectory = repositoryRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new VerifyLocalResult(process.ExitCode, output);
    }

    private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (var sourceFile in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            if (relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(static segment => segment is "bin" or "obj" or "TestResults"))
                continue;

            var destinationFile = Path.Combine(destinationDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
            File.Copy(sourceFile, destinationFile, true);
        }
    }

    private static string[] ReadStringArray(JsonElement arrayElement)
    {
        return arrayElement.EnumerateArray().Select(static item => item.GetString() ?? string.Empty).ToArray();
    }

    private static EvidenceContract[] ReadEvidenceContracts(JsonElement arrayElement)
    {
        return arrayElement.EnumerateArray()
            .Select(static item => new EvidenceContract(
                item.GetProperty("id").GetString() ?? string.Empty,
                item.GetProperty("kind").GetString() ?? string.Empty,
                item.GetProperty("ownerTask").GetString() ?? string.Empty,
                item.GetProperty("path").GetString() ?? string.Empty,
                item.GetProperty("description").GetString() ?? string.Empty))
            .ToArray();
    }

    private sealed record VerifyLocalResult(int ExitCode, string Output);

    private sealed class VerifyLocalFixture(string repositoryRoot) : IDisposable
    {
        public string RepositoryRoot { get; } = repositoryRoot;

        public void Dispose()
        {
            if (Directory.Exists(RepositoryRoot))
                Directory.Delete(RepositoryRoot, true);
        }
    }

    private sealed record EvidenceContract(string Id, string Kind, string OwnerTask, string Path, string Description);
}
