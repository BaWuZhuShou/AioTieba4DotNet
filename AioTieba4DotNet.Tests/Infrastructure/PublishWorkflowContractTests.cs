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
    private static readonly string PublishWorkflowPath = Path.Combine(RepositoryRoot, ".github", "workflows", "publish.yml");
    private static readonly string[] IgnoredPathSegments = [".git", "bin", "obj", "TestResults"];

    private static readonly string TrustedPublishingWorkflowFixture = string.Join(Environment.NewLine, new[]
    {
        "name: Publish to NuGet",
        string.Empty,
        "on:",
        "    push:",
        "        tags:",
        "            - 'v*.*.*'",
        string.Empty,
        "jobs:",
        "    publish:",
        "        permissions:",
        "            contents: write",
        "            id-token: write",
        string.Empty,
        "        steps:",
        "            -   name: Determine release channel",
        "                shell: bash",
        "                run: |",
        "                    if [[ \"${VERSION}\" =~ -(preview|rc)\\.[0-9]+$ ]]; then",
        "                        echo \"PRERELEASE=true\" >> $GITHUB_ENV",
        "                    else",
        "                        echo \"PRERELEASE=false\" >> $GITHUB_ENV",
            "                    fi",
            string.Empty,
            "            -   name: Login to NuGet",
            "                id: login",
            "                uses: NuGet/login@v1",
            string.Empty,
            "            -   name: Push to NuGet",
            string.Concat(
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
        PublishWorkflowContract.AssertSatisfied(TrustedPublishingWorkflowFixture, "trusted publishing workflow fixture");
    }

    [TestMethod]
    public void WorkflowUsingLegacyLongLivedNuGetApiKeySecret_FailsWithConcreteContractMessage()
    {
        var tempDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"publish-workflow-contract-{Guid.NewGuid():N}"));
        var fixturePath = Path.Combine(tempDirectory.FullName, "publish.yml");
        var workflowText = string.Join(Environment.NewLine, new[]
        {
            TrustedPublishingWorkflowFixture,
            "env:",
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

        var segments = relativePath.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);

        foreach (var segment in segments)
        {
            if (Array.Exists(IgnoredPathSegments, ignored => string.Equals(segment, ignored, StringComparison.OrdinalIgnoreCase)))
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
        return $"Repository must not contain a legacy long-lived NuGet API key fallback reference.{Environment.NewLine}- "
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

    private static List<string> ExtractStepBlocks(string publishJobText)
    {
        var lines = NormalizeLineEndings(publishJobText).Split('\n');
        var stepsHeader = new string(' ', 8) + "steps:";
        var stepsIndex = Array.FindIndex(lines, line => string.Equals(line.TrimEnd(), stepsHeader, StringComparison.Ordinal));
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
        => text.Replace("\r\n", "\n", StringComparison.Ordinal);

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

        public const string MissingIdTokenPermissionMessage =
            "publish.yml must request permissions.id-token: write for NuGet trusted publishing.";

        public const string MissingNuGetLoginMessage =
            "publish.yml must include a publish-lane NuGet/login@v1 step with id 'login' before dotnet nuget push.";

        public const string MissingDotnetNuGetPushMessage =
            "publish.yml must keep dotnet nuget push in the publish lane.";

        public const string MissingTrustedPublishingApiKeyWiringMessage =
            "publish.yml must wire dotnet nuget push to \"${{ steps.login.outputs.NUGET_API_KEY }}\" in the publish lane.";

        public const string MissingPrereleaseDetectionMessage =
            "publish.yml must preserve the preview|rc prerelease detection logic.";

        public const string ForbiddenLegacyApiKeyMessage =
            "publish.yml must not reference a legacy long-lived NuGet API key secret.";

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
            {
                violations.Add(MissingTagTriggerMessage);
            }

            if (normalizedWorkflowText.Contains(string.Concat("secrets.", "NUGET_", "API_KEY"), StringComparison.Ordinal))
                violations.Add(ForbiddenLegacyApiKeyMessage);

            var publishJobText = TryExtractIndentedBlock(normalizedWorkflowText, 4, "publish:");
            if (string.IsNullOrWhiteSpace(publishJobText))
            {
                violations.Add(MissingIdTokenPermissionMessage);
                violations.Add(MissingNuGetLoginMessage);
                violations.Add(MissingDotnetNuGetPushMessage);
                violations.Add(MissingTrustedPublishingApiKeyWiringMessage);
                violations.Add(MissingPrereleaseDetectionMessage);
                return [.. violations];
            }

            if (!publishJobText.Contains("id-token: write", StringComparison.Ordinal))
                violations.Add(MissingIdTokenPermissionMessage);

            if (!publishJobText.Contains("-(preview|rc)\\.[0-9]+$", StringComparison.Ordinal))
                violations.Add(MissingPrereleaseDetectionMessage);

            var steps = ExtractStepBlocks(publishJobText);
            var loginStepIndex = steps.FindIndex(step => step.Contains("uses: NuGet/login@v1", StringComparison.Ordinal));
            if (loginStepIndex < 0 || !steps[loginStepIndex].Contains("id: login", StringComparison.Ordinal))
                violations.Add(MissingNuGetLoginMessage);

            var pushStepIndex = steps.FindIndex(step => step.Contains("dotnet nuget push", StringComparison.Ordinal));
            if (pushStepIndex < 0)
            {
                violations.Add(MissingDotnetNuGetPushMessage);
            }
            else if (!steps[pushStepIndex].Contains(TrustedPublishingApiKeyArgument, StringComparison.Ordinal))
            {
                violations.Add(MissingTrustedPublishingApiKeyWiringMessage);
            }

            if (loginStepIndex >= 0 && pushStepIndex >= 0 && loginStepIndex > pushStepIndex)
                violations.Add(MissingNuGetLoginMessage);

            return [.. violations];
        }

        public static void AssertSatisfied(string workflowText, string workflowLabel)
        {
            var violations = GetViolations(workflowText);
            Assert.IsEmpty(violations, FormatFailureMessage(workflowLabel, violations));
        }

        private static string FormatFailureMessage(string workflowLabel, IEnumerable<string> violations)
        {
            return $"{workflowLabel} must satisfy the trusted publishing workflow contract.{Environment.NewLine}- "
                   + string.Join(Environment.NewLine + "- ", violations);
        }
    }
}
