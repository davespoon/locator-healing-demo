using LocatorHealing.Agent.Application;
using LocatorHealing.Agent.Contracts;
using LocatorHealing.Agent.Infrastructure;
using Microsoft.Agents.AI.Workflows;
using AgentWorkflow = Microsoft.Agents.AI.Workflows.Workflow;

namespace LocatorHealing.Agent.Cli;

internal sealed class RunCommandHandler(
    NUnitResultParser resultParser,
    JsonFailureDiagnosticsWriter diagnosticsWriter,
    LocatorHealingReportWriter reportWriter)
{
    public async Task<int> InvokeAsync(
        FileInfo resultsFile,
        DirectoryInfo repoRoot,
        DirectoryInfo? outputDir,
        FileInfo? reportFile)
    {
        if (repoRoot is not null && !repoRoot.Exists)
        {
            Console.Error.WriteLine($"Repository root was not found: {repoRoot.FullName}");
            return 1;
        }

        IReadOnlyList<TestFailureInfo> failures;

        try
        {
            failures = resultParser.ParseFailures(resultsFile.FullName);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error parsing test results:");
            Console.Error.WriteLine(ex.Message);
            return 1;
        }

        if (failures.Count == 0)
        {
            Console.WriteLine("No test failures found in the results file.");
            WriteReportIfRequested(reportFile, []);
            return 0;
        }

        var repoPathResolver = new RepoPathResolver(repoRoot.FullName);

        var failureParser = new SeleniumFailureParser(repoPathResolver);
        var workflow = LocatorHealingWorkflow.Create(repoPathResolver);

        var targetDir = outputDir?.FullName
                        ?? Path.Combine(Path.GetDirectoryName(resultsFile.FullName)!, "error-traces");

        var exitCode = 0;
        var analyzedStates = new List<RepairWorkflowState>();

        foreach (var failure in failures)
        {
            var artifact = failureParser.Parse(failure);
            var diagnosticsPath = diagnosticsWriter.Write(artifact, targetDir);

            var result = await AnalyzeAsync(workflow, diagnosticsPath);

            if (result.State is not null)
            {
                analyzedStates.Add(result.State);
            }

            if (result.ExitCode > exitCode)
            {
                exitCode = result.ExitCode;
            }
        }

        WriteReportIfRequested(reportFile, analyzedStates);

        return exitCode;
    }

    private async Task<AnalysisResult> AnalyzeAsync(AgentWorkflow workflow, string diagnosticsFilePath)
    {
        try
        {
            await using var run = await InProcessExecution.RunStreamingAsync(workflow, input: diagnosticsFilePath);

            await foreach (var evt in run.WatchStreamAsync())
            {
                switch (evt)
                {
                    case WorkflowOutputEvent output when output.Data is RepairWorkflowState state:
                        RepairWorkflowStatePrinter.Print(state);
                        return new AnalysisResult(state.IsStopped ? 2 : 0, state);

                    case WorkflowErrorEvent error:
                        Console.Error.WriteLine("Workflow error:");
                        Console.Error.WriteLine(error.Data);
                        return new AnalysisResult(1, null);
                }
            }

            Console.Error.WriteLine("Workflow completed without output.");
            return new AnalysisResult(1, null);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Unhandled error:");
            Console.Error.WriteLine(ex);
            return new AnalysisResult(1, null);
        }
    }

    private void WriteReportIfRequested(FileInfo? reportFile, IReadOnlyList<RepairWorkflowState> states)
    {
        if (reportFile is null)
        {
            return;
        }

        try
        {
            reportWriter.Write(reportFile.FullName, states);
            Console.WriteLine($"Locator healing report written to: {reportFile.FullName}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Failed to write locator healing report:");
            Console.Error.WriteLine(ex.Message);
        }
    }

    private sealed record AnalysisResult(int ExitCode, RepairWorkflowState? State);
}