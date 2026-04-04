#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Tests.Infrastructure.Contracts;
using AioTieba4DotNet.Tests.Infrastructure.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Online.Suite.Execution;

internal enum OrderedSuiteStageStatus
{
    Succeeded,
    Failed,
    Skipped
}

internal sealed record OrderedSuiteCompensationAggregate(
    int AuditSummaryCount,
    int CleanAuditSummaryCount,
    int FailedCompensationCount,
    int UnreconciledArtifactCount)
{
    public IReadOnlyList<string> ToDisplayLines()
    {
        return
        [
            $"{OnlineSuiteExecutionContract.CompensationAudit} aggregate: audits={AuditSummaryCount}, clean={CleanAuditSummaryCount}, failedCompensations={FailedCompensationCount}, unreconciledArtifacts={UnreconciledArtifactCount}"
        ];
    }
}

internal sealed record OrderedSuiteStageExecutionResult(
    OnlineOrderedSuiteStage Stage,
    OrderedSuiteStageStatus Status,
    int? ExitCode,
    OrderedSuiteCompensationAggregate Compensation,
    string Message);

internal sealed class OrderedSuiteHost
{
    private const string DotNetExecutable = "dotnet";

    private readonly TestContext _testContext;

    public OrderedSuiteHost(TestContext testContext)
    {
        _testContext = testContext ?? throw new ArgumentNullException(nameof(testContext));
    }

    public async Task ExecuteAsync(OnlineOrderedSuiteDefinition suite, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(suite);

        List<OrderedSuiteStageExecutionResult> stageResults = [];
        Exception? failure = null;

        foreach (var stage in suite.Stages.OrderBy(static stage => stage.Order))
        {
            if (failure is not null)
            {
                stageResults.Add(new OrderedSuiteStageExecutionResult(
                    stage,
                    OrderedSuiteStageStatus.Skipped,
                    null,
                    new OrderedSuiteCompensationAggregate(0, 0, 0, 0),
                    "Skipped because an earlier ordered stage failed."));
                continue;
            }

            OrderedSuiteStageExecutionResult result;

            try
            {
                result = await ExecuteStageAsync(stage, cancellationToken);
            }
            catch (Exception exception)
            {
                result = new OrderedSuiteStageExecutionResult(
                    stage,
                    OrderedSuiteStageStatus.Failed,
                    null,
                    new OrderedSuiteCompensationAggregate(0, 0, 0, 0),
                    exception.Message);
            }

            stageResults.Add(result);

            if (result.Status == OrderedSuiteStageStatus.Failed)
                failure = new InvalidOperationException(result.Message);
        }

        WriteSuiteSummary(suite, stageResults);

        if (failure is not null)
            Assert.Fail($"Ordered suite '{suite.SuiteCategory}' failed. See stage summary above for details.");
    }

    private async Task<OrderedSuiteStageExecutionResult> ExecuteStageAsync(
        OnlineOrderedSuiteStage stage,
        CancellationToken cancellationToken)
    {
        _testContext.WriteLine($"{stage.Order}. {stage.StageCategory} [{stage.ProjectName}] - {stage.Description}");
        _testContext.WriteLine($"    {stage.ProjectName} => {stage.TestCategoryFilter}");

        var projectPath = RepositoryPaths.GetProjectFilePath(stage.ProjectName);
        var invocation = await RunDotNetTestAsync(projectPath, stage.TestCategoryFilter, cancellationToken);

        foreach (var line in invocation.OutputLines)
            _testContext.WriteLine($"[{stage.StageCategory}] {line}");

        foreach (var line in invocation.ErrorLines)
            _testContext.WriteLine($"[{stage.StageCategory}:stderr] {line}");

        var compensation = BuildCompensationAggregate(invocation.AllLines);

        if (compensation.AuditSummaryCount == 0)
        {
            return new OrderedSuiteStageExecutionResult(
                stage,
                OrderedSuiteStageStatus.Failed,
                invocation.ExitCode,
                compensation,
                $"Stage '{stage.StageCategory}' completed without any {OnlineSuiteExecutionContract.CompensationAudit} evidence in the captured output.");
        }

        if (compensation.FailedCompensationCount > 0 || compensation.UnreconciledArtifactCount > 0)
        {
            return new OrderedSuiteStageExecutionResult(
                stage,
                OrderedSuiteStageStatus.Failed,
                invocation.ExitCode,
                compensation,
                $"Stage '{stage.StageCategory}' reported compensation audit problems in ordered suite output.");
        }

        if (invocation.ExitCode != 0)
        {
            return new OrderedSuiteStageExecutionResult(
                stage,
                OrderedSuiteStageStatus.Failed,
                invocation.ExitCode,
                compensation,
                $"dotnet test exited with code {invocation.ExitCode} for stage '{stage.StageCategory}'.");
        }

        return new OrderedSuiteStageExecutionResult(
            stage,
            OrderedSuiteStageStatus.Succeeded,
            invocation.ExitCode,
            compensation,
            stage.TestCategoryFilter);
    }

    private void WriteSuiteSummary(
        OnlineOrderedSuiteDefinition suite,
        IReadOnlyList<OrderedSuiteStageExecutionResult> stageResults)
    {
        _testContext.WriteLine($"Ordered suite {suite.SuiteCategory} summary:");

        foreach (var stageResult in stageResults)
        {
            var exitCode = stageResult.ExitCode?.ToString() ?? "n/a";
            _testContext.WriteLine(
                $"  {stageResult.Stage.Order}. {stageResult.Stage.StageCategory} => status={stageResult.Status}, project={stageResult.Stage.ProjectName}, exit={exitCode}, audits={stageResult.Compensation.AuditSummaryCount}, unreconciled={stageResult.Compensation.UnreconciledArtifactCount}, failedCompensations={stageResult.Compensation.FailedCompensationCount}");
        }

        var aggregate = new OrderedSuiteCompensationAggregate(
            stageResults.Sum(static result => result.Compensation.AuditSummaryCount),
            stageResults.Sum(static result => result.Compensation.CleanAuditSummaryCount),
            stageResults.Sum(static result => result.Compensation.FailedCompensationCount),
            stageResults.Sum(static result => result.Compensation.UnreconciledArtifactCount));

        foreach (var line in aggregate.ToDisplayLines())
            _testContext.WriteLine(line);
    }

    private static OrderedSuiteCompensationAggregate BuildCompensationAggregate(IReadOnlyList<string> lines)
    {
        var auditSummaryCount = lines.Count(static line =>
            line.Contains($"{OnlineSuiteExecutionContract.CompensationAudit} summary:", StringComparison.Ordinal));
        var cleanAuditSummaryCount = lines.Count(static line =>
            line.Contains("unreconciled: none", StringComparison.Ordinal)
            || line.Contains("no recorded artifacts or compensation actions.", StringComparison.Ordinal));
        var failedCompensationCount = lines.Count(static line =>
            line.Contains("compensation[Failed]", StringComparison.Ordinal));
        var unreconciledArtifactCount = lines.Count(static line =>
            line.Contains("unreconciled:", StringComparison.Ordinal)
            && !line.Contains("unreconciled: none", StringComparison.Ordinal));

        return new OrderedSuiteCompensationAggregate(
            auditSummaryCount,
            cleanAuditSummaryCount,
            failedCompensationCount,
            unreconciledArtifactCount);
    }

    private static async Task<DotNetTestInvocationResult> RunDotNetTestAsync(
        string projectPath,
        string filterExpression,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = DotNetExecutable,
            WorkingDirectory = RepositoryPaths.FindRepositoryRoot(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("test");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--nologo");
        startInfo.ArgumentList.Add("--logger");
        startInfo.ArgumentList.Add("console;verbosity=detailed");
        startInfo.ArgumentList.Add("--filter");
        startInfo.ArgumentList.Add(filterExpression);

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        using var cancellationRegistration = cancellationToken.Register(static state =>
        {
            var runningProcess = (Process)state!;

            try
            {
                if (!runningProcess.HasExited)
                    runningProcess.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }
        }, process);

        await process.WaitForExitAsync(cancellationToken);

        var output = await outputTask;
        var error = await errorTask;

        return new DotNetTestInvocationResult(
            process.ExitCode,
            SplitLines(output),
            SplitLines(error));
    }

    private static string[] SplitLines(string content)
    {
        return content
            .Split(["\r\n", "\n", "\r"], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToArray();
    }

    private sealed record DotNetTestInvocationResult(
        int ExitCode,
        IReadOnlyList<string> OutputLines,
        IReadOnlyList<string> ErrorLines)
    {
        public IReadOnlyList<string> AllLines => OutputLines.Concat(ErrorLines).ToArray();
    }
}
