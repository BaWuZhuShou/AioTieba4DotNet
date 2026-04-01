#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Infrastructure;

[TestClass]
public sealed class PublishWorkflowContractTests
{
    private const string OverridePathEnvironmentVariable = "AIO_TIEBA_PUBLISH_WORKFLOW_CONTRACT_PATH";
    private const string LegacyApiKeyName = "NUGET_" + "API_KEY";
    private static readonly string LegacySecretReference = string.Concat("secrets.", LegacyApiKeyName);
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    private static readonly string PublishWorkflowPath =
        Path.Combine(RepositoryRoot, ".github", "workflows", "publish.yml");

    private static readonly string[] IgnoredPathSegments = [".git", "bin", "obj", "TestResults"];

    private static readonly string TrustedPublishingWorkflowFixture = string.Join(Environment.NewLine, new[]
    {
        "name: Publish to NuGet", string.Empty, "on:", "    push:", "        tags:", "            - 'v*.*.*'",
        string.Empty, "jobs:", "    release-contracts:", "        steps:",
        "            -   name: Validate local verification contract",
        "                run: bash ./scripts/verify-local.sh --validate-only", string.Empty, "    package:",
        "        needs:", "            - release-contracts", "        steps:",
        "            -   name: Preflight release tag version", "                id: version",
        "                shell: bash", "                run: |",
        "                    if [[ \"${VERSION}\" =~ -(preview|rc)\\.[0-9]+$ ]]; then",
        "                        printf 'prerelease=true\\n' >> \"$GITHUB_OUTPUT\"", "                    fi",
        string.Empty, "            -   name: Pack the project",
        "                run: dotnet pack --configuration Release --no-build --output ./nupkg -p:Version=${{ steps.version.outputs.version }} --nologo",
        string.Empty, "    publish:", "        needs:", "            - package", "        permissions:",
        "            contents: write", "            id-token: write", string.Empty, "        steps:",
        "            -   name: Download package artifacts", "                uses: actions/download-artifact@v4",
        string.Empty, "            -   name: Login to NuGet", "                id: login",
        "                uses: NuGet/login@v1", string.Empty, "            -   name: Push to NuGet", string.Concat(
            "                run: dotnet nuget push ./nupkg/*.nupkg --api-key \"",
            PublishWorkflowContract.TrustedPublishingApiKeyOutputReference,
            "\" --source https://api.nuget.org/v3/index.json --skip-duplicate")
    });

    [TestMethod]
    public void RepositoryPublishWorkflow_MustSatisfyTrustedPublishingContract()
    {
        var workflowText = File.ReadAllText(PublishWorkflowPath);

        PublishWorkflowContract.AssertSatisfied(workflowText, PublishWorkflowPath);
    }

    [TestMethod]
    public void TrustedPublishingWorkflowFixture_RequiredContractIsSatisfied()
    {
        PublishWorkflowContract.AssertSatisfied(TrustedPublishingWorkflowFixture,
            "trusted publishing workflow fixture");
    }

    [TestMethod]
    public void WorkflowUsingLegacyLongLivedNuGetApiKeySecret_FailsWithConcreteContractMessage()
    {
        var tempDirectory =
            Directory.CreateDirectory(Path.Combine(Path.GetTempPath(),
                $"publish-workflow-contract-{Guid.NewGuid():N}"));
        var fixturePath = Path.Combine(tempDirectory.FullName, "publish.yml");
        var workflowText = string.Join(Environment.NewLine,
            new[]
            {
                TrustedPublishingWorkflowFixture, "env:",
                $"    {LegacyApiKeyName}: ${{{{ {LegacySecretReference} }}}}"
            });

        try
        {
            File.WriteAllText(fixturePath, workflowText);

            var exception = Assert.Throws<AssertFailedException>(() =>
                PublishWorkflowContract.AssertSatisfied(File.ReadAllText(fixturePath), fixturePath));

            Assert.Contains(PublishWorkflowContract.ForbiddenLegacyApiKeyMessage, exception.Message);
        }
        finally
        {
            Directory.Delete(tempDirectory.FullName, true);
        }
    }

    [TestMethod]
    public void WorkflowUsingNonTrustedPublishingApiKeyWiring_FailsWithConcreteContractMessage()
    {
        var workflowText = TrustedPublishingWorkflowFixture.Replace(
            PublishWorkflowContract.TrustedPublishingApiKeyOutputReference,
            "${{ env.NUGET_API_KEY }}",
            StringComparison.Ordinal);

        var exception = Assert.Throws<AssertFailedException>(() =>
            PublishWorkflowContract.AssertSatisfied(workflowText, "regressed workflow fixture"));

        Assert.Contains(PublishWorkflowContract.MissingTrustedPublishingApiKeyWiringMessage, exception.Message);
    }

    [TestMethod]
    public void WorkflowRunningDotnetTest_FailsWithConcreteContractMessage()
    {
        var workflowText = TrustedPublishingWorkflowFixture + Environment.NewLine + "    verify:" +
                           Environment.NewLine + "        steps:" + Environment.NewLine +
                           "            -   run: dotnet test";

        var exception = Assert.Throws<AssertFailedException>(() =>
            PublishWorkflowContract.AssertSatisfied(workflowText, "regressed workflow fixture"));

        Assert.Contains(PublishWorkflowContract.ForbiddenDotnetTestMessage, exception.Message);
    }

    [TestMethod]
    public void RepositoryScan_ContainsNoLegacyLongLivedNuGetApiKeySecretReferences()
    {
        var hits = Directory.EnumerateFiles(RepositoryRoot, "*", SearchOption.AllDirectories)
            .Where(path => !IsIgnoredPath(path))
            .Where(ContainsLegacySecretReference)
            .Select(path => Path.GetRelativePath(RepositoryRoot, path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.IsEmpty(hits, FormatRepositoryScanFailureMessage(hits));
    }

    [TestMethod]
    public void OverrideWorkflowPath_WhenProvided_MustSatisfyTrustedPublishingContract()
    {
        var overridePath = Environment.GetEnvironmentVariable(OverridePathEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(overridePath)) return;

        Assert.IsTrue(File.Exists(overridePath),
            $"Environment variable {OverridePathEnvironmentVariable} must point to an existing workflow file.");

        var workflowText = File.ReadAllText(overridePath);

        PublishWorkflowContract.AssertSatisfied(workflowText, overridePath);
    }

    private static bool IsIgnoredPath(string path)
    {
        var relativePath = Path.GetRelativePath(RepositoryRoot, path);
        if (relativePath.StartsWith(Path.Combine(".sisyphus", "plans"), StringComparison.OrdinalIgnoreCase))
            return true;

        var segments = relativePath.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);

        foreach (var segment in segments)
        {
            if (segment.EndsWith("-clone", StringComparison.OrdinalIgnoreCase))
                return true;

            if (Array.Exists(IgnoredPathSegments,
                    ignored => string.Equals(segment, ignored, StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        return false;
    }

    private static bool ContainsLegacySecretReference(string path)
    {
        try
        {
            return File.ReadAllText(path).Contains(LegacySecretReference, StringComparison.Ordinal);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static string FormatRepositoryScanFailureMessage(IEnumerable<string> hits)
    {
        return
            $"Repository must not contain a legacy long-lived NuGet API key fallback reference.{Environment.NewLine}- "
            + string.Join(Environment.NewLine + "- ", hits);
    }

    private static string? TryExtractIndentedBlock(string text, int indentation, string header)
    {
        var lines = NormalizeLineEndings(text).Split('\n');
        var expectedLine = new string(' ', indentation) + header;

        for (var i = 0; i < lines.Length; i++)
        {
            if (!string.Equals(lines[i].TrimEnd(), expectedLine, StringComparison.Ordinal)) continue;

            var end = lines.Length;
            for (var j = i + 1; j < lines.Length; j++)
            {
                if (string.IsNullOrWhiteSpace(lines[j])) continue;

                if (CountIndentation(lines[j]) <= indentation)
                {
                    end = j;
                    break;
                }
            }

            return string.Join(Environment.NewLine, lines[i..end]);
        }

        return null;
    }

    private static List<string> ExtractStepBlocks(string jobText)
    {
        var lines = NormalizeLineEndings(jobText).Split('\n');
        var stepsHeader = new string(' ', 8) + "steps:";
        var stepsIndex = Array.FindIndex(lines,
            line => string.Equals(line.TrimEnd(), stepsHeader, StringComparison.Ordinal));
        if (stepsIndex < 0) return [];

        List<string> steps = [];
        int? currentStepStart = null;

        for (var i = stepsIndex + 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var indentation = CountIndentation(lines[i]);
            if (indentation <= 8) break;

            if (indentation == 12 && lines[i].TrimStart().StartsWith("-", StringComparison.Ordinal))
            {
                if (currentStepStart is int start)
                    steps.Add(string.Join(Environment.NewLine, lines[start..i]));

                currentStepStart = i;
            }
        }

        if (currentStepStart is int lastStart)
            steps.Add(string.Join(Environment.NewLine, lines[lastStart..]));

        return steps;
    }

    private static int CountIndentation(string line)
    {
        var count = 0;
        while (count < line.Length && line[count] == ' ')
            count++;

        return count;
    }

    private static string NormalizeLineEndings(string text)
    {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
        {
            var candidate = Path.Combine(current.FullName, ".github", "workflows", "publish.yml");
            if (File.Exists(candidate)) return current.FullName;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate repository root from test base directory '{AppContext.BaseDirectory}'.");
    }

    private static class PublishWorkflowContract
    {
        public const string MissingTagTriggerMessage =
            "publish.yml must keep the v*.*.* tag trigger for releases.";

        public const string MissingReleaseContractJobMessage =
            "publish.yml must define a release-contracts job before packaging.";

        public const string MissingReleaseContractValidationMessage =
            "publish.yml must validate docs and local evidence via scripts/verify-local.sh --validate-only.";

        public const string MissingPackageJobMessage =
            "publish.yml must package release artifacts in a dedicated package job.";

        public const string MissingPackageNeedsReleaseContractsMessage =
            "publish.yml must make the package job depend on release-contracts so pack never runs before docs and evidence checks.";

        public const string MissingPublishNeedsPackageMessage =
            "publish.yml must make the publish job depend on package outputs.";

        public const string MissingIdTokenPermissionMessage =
            "publish.yml must request permissions.id-token: write for NuGet trusted publishing.";

        public const string MissingNuGetLoginMessage =
            "publish.yml must include a publish-lane NuGet/login@v1 step with id 'login'.";

        public const string MissingImmediateNuGetLoginMessage =
            "publish.yml must keep NuGet/login@v1 immediately before dotnet nuget push.";

        public const string MissingDotnetNuGetPushMessage =
            "publish.yml must keep dotnet nuget push in the publish lane.";

        public const string MissingTrustedPublishingApiKeyWiringMessage =
            "publish.yml must wire dotnet nuget push to \"${{ steps.login.outputs.NUGET_API_KEY }}\" in the publish lane.";

        public const string MissingPrereleaseDetectionMessage =
            "publish.yml must preserve the preview|rc prerelease detection logic.";

        public const string ForbiddenLegacyApiKeyMessage =
            "publish.yml must not reference a legacy long-lived NuGet API key secret.";

        public const string ForbiddenDotnetTestMessage =
            "publish.yml must not run dotnet test in GitHub Actions.";

        public static readonly string TrustedPublishingApiKeyOutputReference =
            string.Concat("${{ steps.login.outputs.", LegacyApiKeyName, " }}");

        private static readonly string TrustedPublishingApiKeyArgument =
            string.Concat("--api-key \"", TrustedPublishingApiKeyOutputReference, "\"");

        public static string[] GetViolations(string workflowText)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(workflowText);

            var normalizedWorkflowText = NormalizeLineEndings(workflowText);
            List<string> violations = [];

            if (!normalizedWorkflowText.Contains("push:", StringComparison.Ordinal)
                || !normalizedWorkflowText.Contains("tags:", StringComparison.Ordinal)
                || !normalizedWorkflowText.Contains("v*.*.*", StringComparison.Ordinal))
                violations.Add(MissingTagTriggerMessage);

            if (normalizedWorkflowText.Contains(string.Concat("secrets.", "NUGET_", "API_KEY"),
                    StringComparison.Ordinal))
                violations.Add(ForbiddenLegacyApiKeyMessage);

            if (normalizedWorkflowText.Contains("dotnet test", StringComparison.Ordinal))
                violations.Add(ForbiddenDotnetTestMessage);

            var releaseContractsJobText = TryExtractIndentedBlock(normalizedWorkflowText, 4, "release-contracts:");
            if (string.IsNullOrWhiteSpace(releaseContractsJobText))
            {
                violations.Add(MissingReleaseContractJobMessage);
                violations.Add(MissingReleaseContractValidationMessage);
            }
            else if (!releaseContractsJobText.Contains("bash ./scripts/verify-local.sh --validate-only",
                         StringComparison.Ordinal))
            {
                violations.Add(MissingReleaseContractValidationMessage);
            }

            var packageJobText = TryExtractIndentedBlock(normalizedWorkflowText, 4, "package:");
            if (string.IsNullOrWhiteSpace(packageJobText))
            {
                violations.Add(MissingPackageJobMessage);
                violations.Add(MissingPackageNeedsReleaseContractsMessage);
                violations.Add(MissingPrereleaseDetectionMessage);
            }
            else
            {
                if (!packageJobText.Contains("needs:", StringComparison.Ordinal)
                    || !packageJobText.Contains("release-contracts", StringComparison.Ordinal))
                    violations.Add(MissingPackageNeedsReleaseContractsMessage);

                if (!packageJobText.Contains("dotnet pack", StringComparison.Ordinal))
                    violations.Add(MissingPackageJobMessage);

                if (!packageJobText.Contains("-(preview|rc)\\.[0-9]+$", StringComparison.Ordinal))
                    violations.Add(MissingPrereleaseDetectionMessage);
            }

            var publishJobText = TryExtractIndentedBlock(normalizedWorkflowText, 4, "publish:");
            if (string.IsNullOrWhiteSpace(publishJobText))
            {
                violations.Add(MissingPublishNeedsPackageMessage);
                violations.Add(MissingIdTokenPermissionMessage);
                violations.Add(MissingNuGetLoginMessage);
                violations.Add(MissingImmediateNuGetLoginMessage);
                violations.Add(MissingDotnetNuGetPushMessage);
                violations.Add(MissingTrustedPublishingApiKeyWiringMessage);
                return [.. violations];
            }

            if (!publishJobText.Contains("needs:", StringComparison.Ordinal)
                || !publishJobText.Contains("package", StringComparison.Ordinal))
                violations.Add(MissingPublishNeedsPackageMessage);

            if (!publishJobText.Contains("id-token: write", StringComparison.Ordinal))
                violations.Add(MissingIdTokenPermissionMessage);

            var steps = ExtractStepBlocks(publishJobText);
            var loginStepIndex =
                steps.FindIndex(step => step.Contains("uses: NuGet/login@v1", StringComparison.Ordinal));
            if (loginStepIndex < 0 || !steps[loginStepIndex].Contains("id: login", StringComparison.Ordinal))
                violations.Add(MissingNuGetLoginMessage);

            var pushStepIndex = steps.FindIndex(step => step.Contains("dotnet nuget push", StringComparison.Ordinal));
            if (pushStepIndex < 0)
                violations.Add(MissingDotnetNuGetPushMessage);
            else if (!steps[pushStepIndex].Contains(TrustedPublishingApiKeyArgument, StringComparison.Ordinal))
                violations.Add(MissingTrustedPublishingApiKeyWiringMessage);

            if (loginStepIndex < 0 || pushStepIndex < 0 || pushStepIndex != loginStepIndex + 1)
                violations.Add(MissingImmediateNuGetLoginMessage);

            return [.. violations.Distinct(StringComparer.Ordinal)];
        }

        public static void AssertSatisfied(string workflowText, string workflowLabel)
        {
            var violations = GetViolations(workflowText);
            Assert.IsEmpty(violations, FormatFailureMessage(workflowLabel, violations));
        }

        private static string FormatFailureMessage(string workflowLabel, IEnumerable<string> violations)
        {
            return
                $"{workflowLabel} must satisfy the trusted publishing and release governance contract.{Environment.NewLine}- "
                + string.Join(Environment.NewLine + "- ", violations);
        }
    }
}
